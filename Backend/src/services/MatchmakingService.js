/**
 * Matchmaking Service
 * Handles player queue, team formation, and match assignment
 */
class MatchmakingService {
  constructor(io, redis, logger) {
    this.io = io;
    this.redis = redis;
    this.logger = logger;

    // In-memory queues (in production: Redis sorted sets)
    this.queues = {
      solo: [],
      duos: [],
      squads: []
    };

    // Matchmaking interval
    this.matchInterval = setInterval(() => this.processQueues(), 3000);
    this.MATCH_TIMEOUT = parseInt(process.env.MATCH_QUEUE_TIMEOUT) || 120;
    this.MAX_PLAYERS = parseInt(process.env.MAX_PLAYERS_PER_MATCH) || 60;
  }

  /**
   * Add player to matchmaking queue
   */
  joinQueue(socket, data) {
    const mode = data?.mode || 'solo';
    const team = data?.team || [];

    if (!this.queues[mode]) {
      socket.emit('match:error', { message: 'Invalid game mode', code: 'INVALID_MODE' });
      return;
    }

    // Remove from any existing queue
    this.leaveQueue(socket);

    const entry = {
      socketId: socket.id,
      playerId: socket.playerId,
      playerName: socket.playerName,
      ping: socket.pingTime || 0,
      mmr: data?.mmr || 1000,
      joinTime: Date.now(),
      mode,
      team
    };

    this.queues[mode].push(entry);
    socket.join(`queue:${mode}`);

    const queueSize = this.queues[mode].length;
    socket.emit('match:queued', {
      mode,
      position: queueSize,
      estimatedWait: this.estimateWaitTime(mode, queueSize)
    });

    this.logger.info(`🎯 ${socket.playerName} joined ${mode} queue (${queueSize} waiting)`);
  }

  /**
   * Remove player from queue
   */
  leaveQueue(socket) {
    for (const mode of Object.keys(this.queues)) {
      const before = this.queues[mode].length;
      this.queues[mode] = this.queues[mode].filter(e => e.socketId !== socket.id);
      if (this.queues[mode].length < before) {
        socket.leave(`queue:${mode}`);
        socket.emit('match:dequeued', { mode });
        return;
      }
    }
  }

  /**
   * Player confirmed ready for match
   */
  matchReady(socket, data) {
    // Would handle ready-check confirmation
    socket.emit('match:countdown', { seconds: 5 });
  }

  /**
   * Process all queues and create matches
   */
  processQueues() {
    for (const [mode, queue] of Object.entries(this.queues)) {
      if (queue.length < 1) continue; // Need at least 1 waiting player

      const teamSize = mode === 'solo' ? 1 : mode === 'duos' ? 2 : 4;
      const oldestEntry = queue[0];
      const queueAgeSec = oldestEntry ? (Date.now() - oldestEntry.joinTime) / 1000 : 0;
      
      // Force start with bot fill when 5-min timer (or configured timeout) is reached, or when full
      const timeoutReached = queueAgeSec >= Math.min(300, this.MATCH_TIMEOUT);
      if (queue.length >= this.MAX_PLAYERS || timeoutReached) {
        this.createMatch(mode, queue, teamSize);
      }
    }
  }

  /**
   * Create a match from queued players, filling remaining slots up to 60 with AI Bots.
   */
  createMatch(mode, queue, teamSize) {
    const matchId = `match_${Date.now()}_${Math.random().toString(36).substr(2, 6)}`;
    const humanPlayers = queue.splice(0, Math.min(queue.length, this.MAX_PLAYERS));
    const serverUrl = this.selectGameServer();

    // Generate AI Bots to fill up remaining slots to reach exactly MAX_PLAYERS (60)
    const neededBots = Math.max(0, this.MAX_PLAYERS - humanPlayers.length);
    const bots = [];
    for (let i = 0; i < neededBots; i++) {
      bots.push({
        playerId: `bot_${matchId}_${i}`,
        playerName: `Apex_AI_${Math.floor(Math.random()*9000)+1000}`,
        ping: 12,
        mmr: 1100,
        isBot: true
      });
    }

    const allCombatants = [...humanPlayers, ...bots];
    const teams = this.assignTeams(allCombatants, teamSize);

    // Notify human players
    for (const player of humanPlayers) {
      const socket = this.getSocketById(player.socketId);
      if (socket) {
        socket.emit('match:found', {
          matchId,
          serverUrl,
          mode,
          playerCount: allCombatants.length,
          humanCount: humanPlayers.length,
          botCount: neededBots,
          players: allCombatants.map(p => ({
            playerId: p.playerId,
            playerName: p.playerName,
            ping: p.ping,
            isBot: !!p.isBot
          })),
          team: teams.find(t => t.includes(player.playerId)) || [player.playerId],
          countdown: 5
        });
        socket.leave(`queue:${mode}`);
      }
    }

    // Register active session in global memory store & supervision
    if (global.memoryStore && Array.isArray(global.memoryStore.matches)) {
      global.memoryStore.matches.unshift({
        matchId,
        mode,
        map: 'Orbital Relay Station',
        playerCount: allCombatants.length,
        humanCount: humanPlayers.length,
        botCount: neededBots,
        duration: 0,
        endedAt: new Date().toISOString()
      });
      if (global.memoryStore.matches.length > 50) global.memoryStore.matches.pop();
    }

    this.logger.info(`🎮 Match created: ${matchId} (${mode}) - ${humanPlayers.length} humans + ${neededBots} bots paired (Total: ${allCombatants.length})`);
    return matchId;
  }

  /**
   * Assign players to teams
   */
  assignTeams(players, teamSize) {
    const teams = [];
    if (teamSize === 1) {
      // Solo: each player is their own team
      return players.map(p => [p.playerId]);
    }

    // Shuffle and distribute
    const shuffled = [...players].sort(() => Math.random() - 0.5);
    for (let i = 0; i < shuffled.length; i += teamSize) {
      const team = shuffled.slice(i, i + teamSize).map(p => p.playerId);
      teams.push(team);
    }
    return teams;
  }

  /**
   * Estimate wait time based on queue state
   */
  estimateWaitTime(mode, queueSize) {
    const needed = this.MAX_PLAYERS - queueSize;
    const joinRate = 2; // Average players joining per second
    return Math.max(5, Math.ceil(needed / joinRate));
  }

  /**
   * Select a game server (in production: from server pool)
   */
  selectGameServer() {
    return process.env.GAME_SERVER_URL || 'ws://localhost:3001';
  }

  /**
   * Get socket by ID
   */
  getSocketById(socketId) {
    return this.io.sockets?.sockets?.get(socketId) || null;
  }

  /**
   * Clean up stale entries
   */
  cleanup() {
    const now = Date.now();
    for (const mode of Object.keys(this.queues)) {
      this.queues[mode] = this.queues[mode].filter(e => {
        const age = (now - e.joinTime) / 1000;
        if (age > this.MATCH_TIMEOUT * 2) {
          const socket = this.getSocketById(e.socketId);
          if (socket) socket.emit('match:timeout', { message: 'Queue timed out' });
          return false;
        }
        return true;
      });
    }
  }
}

module.exports = MatchmakingService;
