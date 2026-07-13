const express = require('express');
const http = require('http');
const { Server } = require('socket.io');
const mongoose = require('mongoose');
const Redis = require('ioredis');
const helmet = require('helmet');
const cors = require('cors');
const compression = require('compression');
const morgan = require('morgan');
const rateLimit = require('express-rate-limit');
const { createLogger, format, transports } = require('winston');
require('dotenv').config();

// ─── Logger Setup ──────────────────────────────────────────────
const logger = createLogger({
  level: process.env.LOG_LEVEL || 'info',
  format: format.combine(
    format.timestamp({ format: 'YYYY-MM-DD HH:mm:ss' }),
    format.errors({ stack: true }),
    format.json()
  ),
  transports: [
    new transports.Console({
      format: format.combine(format.colorize(), format.simple())
    }),
    new transports.File({ 
      filename: process.env.LOG_FILE || 'logs/arenafall.log',
      maxsize: 5242880,
      maxFiles: 5
    })
  ]
});

// ─── Config ────────────────────────────────────────────────────
const config = {
  port: parseInt(process.env.PORT) || 3000,
  mongoUri: process.env.MONGODB_URI || process.env.MONGO_URI || process.env.MONGO_URL || process.env.DATABASE_URL || process.env.DB_URI || process.env.MONGODB_URL || 'mongodb://localhost:27017/arenafall',
  redisUrl: process.env.REDIS_URL || process.env.REDIS_URI || process.env.REDIS || 'redis://localhost:6379',
  jwtSecret: process.env.JWT_SECRET || 'arenafall-dev-secret-key-2024',
  jwtRefreshSecret: process.env.JWT_REFRESH_SECRET || 'arenafall-refresh-secret',
  jwtExpiry: process.env.JWT_EXPIRY || '24h',
  jwtRefreshExpiry: process.env.JWT_REFRESH_EXPIRY || '7d',
  apiVersion: process.env.API_VERSION || 'v1',
  maxPlayersPerMatch: parseInt(process.env.MAX_PLAYERS_PER_MATCH) || 60,
  matchQueueTimeout: parseInt(process.env.MATCH_QUEUE_TIMEOUT) || 120,
  antiCheatEnabled: process.env.ANTICHEAT_ENABLED === 'true',
  encryptionKey: process.env.ENCRYPTION_KEY || 'default-key-32-chars-long!',
  encryptionIv: process.env.ENCRYPTION_IV || 'default-iv-16-ch'
};

// ─── Express App ───────────────────────────────────────────────
const app = express();
const server = http.createServer(app);

// Security middleware
app.use(helmet({
  contentSecurityPolicy: false,
  crossOriginEmbedderPolicy: false
}));
app.use(cors({
  origin: '*',
  methods: ['GET', 'POST', 'PUT', 'DELETE', 'PATCH'],
  allowedHeaders: ['Content-Type', 'Authorization', 'X-Session-Token']
}));
app.use(compression());
app.use(express.json({ limit: '10mb' }));
app.use(express.urlencoded({ extended: true, limit: '10mb' }));
app.use(morgan('short'));

// Global rate limiter
const limiter = rateLimit({
  windowMs: parseInt(process.env.RATE_LIMIT_WINDOW_MS) || 900000,
  max: parseInt(process.env.RATE_LIMIT_MAX) || 100,
  message: { error: 'Too many requests', code: 'RATE_LIMIT' },
  standardHeaders: true,
  legacyHeaders: false
});
app.use('/api/', limiter);

// ─── Socket.IO Setup ───────────────────────────────────────────
const io = new Server(server, {
  cors: { origin: '*', methods: ['GET', 'POST'] },
  pingInterval: 25000,
  pingTimeout: 20000,
  maxHttpBufferSize: 1e6
});

// ─── Database Connections ──────────────────────────────────────
let dbConnected = false;
let redisClient = null;
global.mongoLastError = null;

async function connectDatabases() {
  try {
    await mongoose.connect(config.mongoUri, {
      maxPoolSize: 50,
      serverSelectionTimeoutMS: 8000,
      heartbeatFrequencyMS: 10000
    });
    dbConnected = true;
    global.mongoLastError = null;
    logger.info('✅ MongoDB connected');
  } catch (err) {
    dbConnected = false;
    global.mongoLastError = err.message;
    logger.warn('⚠️ MongoDB connection failed (server running authoritatively in memory without DB): ' + err.message);
  }

  try {
    redisClient = new Redis(config.redisUrl, {
      maxRetriesPerRequest: 3,
      retryStrategy: (times) => Math.min(times * 50, 2000)
    });
    redisClient.on('connect', () => logger.info('✅ Redis connected'));
    redisClient.on('error', (err) => logger.warn('⚠️ Redis error: ' + err.message));
  } catch (err) {
    logger.warn('⚠️ Redis connection failed (server will run without Redis)');
  }
}

// ─── Static Web Frontend & Admin Dashboard ─────────────────────
const path = require('path');
app.use('/admin', express.static(path.join(__dirname, '../admin-dashboard')));
app.use('/', express.static(path.join(__dirname, '../web-frontend')));

// ─── Health Check ──────────────────────────────────────────────
const healthHandler = (req, res) => {
  res.json({
    status: 'online',
    version: config.apiVersion,
    uptime: process.uptime(),
    timestamp: new Date().toISOString(),
    services: {
      database: dbConnected ? 'connected' : 'disconnected',
      mongoError: dbConnected ? null : (global.mongoLastError || 'Check MONGODB_URI or MongoDB Atlas IP Whitelist (0.0.0.0/0)'),
      redis: redisClient?.status === 'ready' ? 'connected' : 'disconnected'
    },
    game: 'Arena Fall Battle Royale'
  });
};
app.get('/health', healthHandler);
app.get(`/api/${config.apiVersion}/health`, healthHandler);

// ─── Import Routes ─────────────────────────────────────────────
const authRoutes = require('./routes/auth');
const playerRoutes = require('./routes/player');
const matchRoutes = require('./routes/match');
const leaderboardRoutes = require('./routes/leaderboard');
const socialRoutes = require('./routes/social');
const analyticsRoutes = require('./routes/analytics');
const shopRoutes = require('./routes/shop');
const tournamentRoutes = require('./routes/tournaments');
const seasonRoutes = require('./routes/seasons');
const replayRoutes = require('./routes/replays');
const supervisionRoutes = require('./routes/supervision');

// Pass redisClient to routes that need it after connection
const originalConnect = connectDatabases;
connectDatabases = async function() {
  await originalConnect();
  if (redisClient) leaderboardRoutes.setRedis?.(redisClient);
};

app.use(`/api/${config.apiVersion}/auth`, authRoutes);
app.use(`/api/${config.apiVersion}/players`, playerRoutes);
app.use(`/api/${config.apiVersion}/matches`, matchRoutes);
app.use(`/api/${config.apiVersion}/leaderboard`, leaderboardRoutes);
app.use(`/api/${config.apiVersion}/social`, socialRoutes);
app.use(`/api/${config.apiVersion}/analytics`, analyticsRoutes);
app.use(`/api/${config.apiVersion}/shop`, shopRoutes);
app.use(`/api/${config.apiVersion}/tournaments`, tournamentRoutes);
app.use(`/api/${config.apiVersion}/seasons`, seasonRoutes);
app.use(`/api/${config.apiVersion}/replays`, replayRoutes);
app.use(`/api/${config.apiVersion}/supervision`, supervisionRoutes);

// ─── Socket.IO Handlers ────────────────────────────────────────
const MatchmakingService = require('./services/MatchmakingService');
const GameSessionService = require('./services/GameSessionService');
const AntiCheatService = require('./services/AntiCheatService');
const BackendSupervisionService = require('./services/BackendSupervisionService');

const matchmakingService = new MatchmakingService(io, redisClient, logger);
const gameSessionService = new GameSessionService(io, redisClient, logger);
const antiCheatService = new AntiCheatService(logger);
const supervisionService = new BackendSupervisionService(io, redisClient, logger);

supervisionRoutes.setSupervisionService(supervisionService);
antiCheatService.setSupervisionService(supervisionService);

io.use((socket, next) => {
  const token = socket.handshake.auth?.token;
  if (!token) return next(new Error('Authentication required'));
  try {
    const jwt = require('jsonwebtoken');
    const decoded = jwt.verify(token, config.jwtSecret);
    socket.playerId = decoded.playerId;
    socket.playerName = decoded.playerName;
    next();
  } catch (err) {
    next(new Error('Invalid token'));
  }
});

io.on('connection', (socket) => {
  logger.info(`🔌 Player connected: ${socket.playerName} (${socket.id})`);

  socket.on('match:join', (data) => matchmakingService.joinQueue(socket, data));
  socket.on('match:leave', () => matchmakingService.leaveQueue(socket));
  socket.on('match:ready', (data) => matchmakingService.matchReady(socket, data));

  socket.on('game:state', (data) => gameSessionService.handleGameState(socket, data));
  socket.on('game:action', (data) => gameSessionService.handleAction(socket, data));
  socket.on('game:chat', (data) => gameSessionService.handleChat(socket, data));

  socket.on('anticheat:report', (data) => antiCheatService.report(socket, data));
  socket.on('pong', (data) => { socket.pingTime = Date.now() - data; });

  socket.on('disconnect', (reason) => {
    logger.info(`🔌 Player disconnected: ${socket.playerName} (${reason})`);
    matchmakingService.leaveQueue(socket);
    gameSessionService.handleDisconnect(socket);
  });
});

// ─── Error Handler ─────────────────────────────────────────────
app.use((err, req, res, next) => {
  logger.error('Unhandled error:', err);
  res.status(err.status || 500).json({
    error: err.message || 'Internal server error',
    code: err.code || 'INTERNAL_ERROR'
  });
});

// ─── Start Server ──────────────────────────────────────────────
async function start() {
  await connectDatabases();

  // Pass redisClient to routes that need it
  if (typeof leaderboardRoutes.setRedis === 'function') {
    leaderboardRoutes.setRedis(redisClient);
  }

  server.listen(config.port, '0.0.0.0', () => {
    logger.info('═══════════════════════════════════════════');
    logger.info('  🎮 Arena Fall Backend Server v1.0.0');
    logger.info(`  📡 Port: ${config.port}`);
    logger.info(`  🔧 Environment: ${process.env.NODE_ENV || 'development'}`);
    logger.info(`  🗄️  MongoDB: ${dbConnected ? '✅' : '⚠️'}`);
    logger.info(`  ⚡ Redis: ${redisClient?.status === 'ready' ? '✅' : '⚠️'}`);
    logger.info(`  🛡️  Anti-Cheat: ${config.antiCheatEnabled ? '✅' : '⚠️'}`);
    logger.info('═══════════════════════════════════════════');
  });
}

start();

module.exports = { app, server, io, logger, config, redisClient };
