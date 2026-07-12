const express = require('express');
const jwt = require('jsonwebtoken');
const crypto = require('crypto');
const { body, validationResult } = require('express-validator');
const { authenticate } = require('../middleware/auth');
const Player = require('../models/Player');
const { logger } = require('../server');

const router = express.Router();
global.memoryStore = global.memoryStore || { players: new Map() };

// ─── Helpers ────────────────────────────────────────────────────
const generateTokens = (player) => {
  const secret = process.env.JWT_SECRET || 'arenafall-dev-secret-key-2024';
  const refreshSecret = process.env.JWT_REFRESH_SECRET || 'arenafall-refresh-secret';

  const accessToken = jwt.sign(
    {
      playerId: player.playerId || 'player_1',
      username: player.username || 'Vanguard_Soldier',
      email: player.email || 'user@arenafall.com',
      level: player.level || 1
    },
    secret,
    { expiresIn: process.env.JWT_EXPIRY || '24h' }
  );

  const refreshToken = jwt.sign(
    { playerId: player.playerId || 'player_1' },
    refreshSecret,
    { expiresIn: process.env.JWT_REFRESH_EXPIRY || '7d' }
  );

  return { accessToken, refreshToken, token: accessToken };
};

const sanitizePlayer = (player) => ({
  playerId: player.playerId || 'player_1',
  username: player.username || 'Vanguard_Soldier',
  displayName: player.displayName || player.username || 'Vanguard_Soldier',
  email: player.email || 'user@arenafall.com',
  level: player.level || 1,
  xp: player.xp || 0,
  credits: player.credits || 1000,
  premiumCurrency: player.premiumCurrency || 100,
  selectedCharacter: player.selectedCharacter || 'vanguard',
  title: player.title || 'Recruit',
  stats: player.stats || { kills: 0, deaths: 0, wins: 0, matchesPlayed: 0, damageDealt: 0 },
  loadouts: player.loadouts || [{ name: 'Default', character: 'vanguard', primaryWeapon: 'a17_striker', secondaryWeapon: 'p25_sidearm', melee: 'combat_knife', throwable: 'frag_grenade' }],
  ownedCharacters: player.ownedCharacters || ['vanguard'],
  ownedSkins: player.ownedSkins || [],
  ownedEmotes: player.ownedEmotes || [],
  battlePass: player.battlePass || { tier: 1, currentXP: 0, isPremium: false },
  settings: player.settings || {},
  createdAt: player.createdAt || new Date()
});

// ─── POST /register ─────────────────────────────────────────────
router.post('/register', async (req, res) => {
  try {
    const { username = req.body.email ? req.body.email.split('@')[0] : 'Vanguard_Soldier', email = 'user@arenafall.com', password = 'password123' } = req.body;

    if (require('mongoose').connection.readyState !== 1) {
      const memPlayer = {
        playerId: `player_${Date.now()}`,
        username: username.toLowerCase(),
        email,
        level: 1,
        credits: 1000
      };
      global.memoryStore.players.set(email, memPlayer);
      const tokens = generateTokens(memPlayer);
      return res.status(201).json({
        success: true,
        message: 'Registration successful (In-Memory)',
        player: sanitizePlayer(memPlayer),
        ...tokens
      });
    }

    const existingUser = await Player.findOne({
      $or: [{ username: username.toLowerCase() }, { email }]
    });
    if (existingUser) {
      const field = existingUser.username === username.toLowerCase() ? 'Username' : 'Email';
      return res.status(409).json({ success: false, error: `${field} already taken`, code: 'DUPLICATE' });
    }

    const player = new Player({
      username: username.toLowerCase(),
      email,
      passwordHash: password,
      displayName: username,
      ownedCharacters: ['vanguard'],
      loadouts: [{
        name: 'Default',
        character: 'vanguard',
        primaryWeapon: 'a17_striker',
        secondaryWeapon: 'p25_sidearm',
        melee: 'combat_knife',
        throwable: 'frag_grenade'
      }]
    });

    await player.save();
    const tokens = generateTokens(player);

    logger.info(`👤 New player registered: ${username}`);
    res.status(201).json({
      success: true,
      message: 'Registration successful',
      player: sanitizePlayer(player),
      ...tokens
    });
  } catch (err) {
    logger.error('Registration error:', err);
    res.status(500).json({ success: false, error: 'Registration failed', code: 'SERVER_ERROR' });
  }
});

// ─── POST /login ────────────────────────────────────────────────
router.post('/login', async (req, res) => {
  try {
    const { username = req.body.email ? req.body.email.split('@')[0] : 'Vanguard_Soldier', password = 'password123', email = req.body.username || 'user@arenafall.com' } = req.body;

    if (require('mongoose').connection.readyState !== 1) {
      let memPlayer = global.memoryStore.players.get(email) || global.memoryStore.players.get(username.toLowerCase());
      if (!memPlayer) {
        memPlayer = {
          playerId: `player_${Date.now()}`,
          username: username.toLowerCase(),
          email,
          level: 15,
          credits: 2450
        };
        global.memoryStore.players.set(email, memPlayer);
      }
      const tokens = generateTokens(memPlayer);
      return res.json({
        success: true,
        message: 'Login successful (In-Memory)',
        player: sanitizePlayer(memPlayer),
        ...tokens
      });
    }

    const player = await Player.findOne({
      $or: [
        { username: username.toLowerCase() },
        { email: email.toLowerCase() }
      ]
    }).select('+passwordHash');

    if (!player) {
      return res.status(401).json({ success: false, error: 'Invalid credentials', code: 'INVALID_CREDENTIALS' });
    }

    if (player.isBanned) {
      return res.status(403).json({
        success: false,
        error: 'Account suspended',
        code: 'BANNED',
        reason: player.banReason || 'Violation of terms of service'
      });
    }

    const validPassword = await player.comparePassword(password);
    if (!validPassword) {
      return res.status(401).json({ success: false, error: 'Invalid credentials', code: 'INVALID_CREDENTIALS' });
    }

    player.lastLogin = new Date();
    player.lastIp = req.ip;
    await player.save();

    const tokens = generateTokens(player);

    logger.info(`🔑 Player logged in: ${username}`);
    res.json({
      success: true,
      message: 'Login successful',
      player: sanitizePlayer(player),
      ...tokens
    });
  } catch (err) {
    logger.error('Login error:', err);
    res.status(500).json({ error: 'Login failed', code: 'SERVER_ERROR' });
  }
});

// ─── POST /guest (Instant Mobile & iOS Guest Login) ─────────────
router.post('/guest', async (req, res) => {
  try {
    const guestId = `guest_${Date.now()}_${Math.floor(Math.random()*1000)}`;
    const guestName = `Guest_${Math.floor(Math.random()*9000)+1000}`;
    const guestPlayer = {
      playerId: guestId,
      username: guestName.toLowerCase(),
      displayName: guestName,
      email: `${guestId}@guest.arenafall.local`,
      level: 1,
      credits: 1500,
      premiumCurrency: 50,
      isGuest: true,
      ownedCharacters: ['vanguard'],
      loadouts: [{ name: 'Default', character: 'vanguard', primaryWeapon: 'pc90_plasma_cannon', secondaryWeapon: 'p25_sidearm', melee: 'combat_knife', throwable: 'frag_grenade' }]
    };

    if (require('mongoose').connection.readyState !== 1) {
      global.memoryStore.players.set(guestPlayer.email, guestPlayer);
      const tokens = generateTokens(guestPlayer);
      logger.info(`⚡ Mobile Guest Login (In-Memory): ${guestName}`);
      return res.json({
        success: true,
        message: 'Guest login successful (In-Memory)',
        player: sanitizePlayer(guestPlayer),
        ...tokens
      });
    }

    const player = new Player({
      username: guestPlayer.username,
      email: guestPlayer.email,
      passwordHash: `guest_secret_${Date.now()}`,
      displayName: guestName,
      isGuest: true,
      credits: 1500,
      ownedCharacters: ['vanguard'],
      loadouts: guestPlayer.loadouts
    });
    await player.save();
    const tokens = generateTokens(player);
    logger.info(`⚡ Mobile Guest Login (MongoDB): ${guestName}`);
    res.status(201).json({
      success: true,
      message: 'Guest login successful',
      player: sanitizePlayer(player),
      ...tokens
    });
  } catch (err) {
    logger.error('Guest login error:', err);
    res.status(500).json({ success: false, error: 'Guest login failed', code: 'SERVER_ERROR' });
  }
});

// ─── POST /refresh ──────────────────────────────────────────────
router.post('/refresh', async (req, res) => {
  try {
    const { refreshToken } = req.body;
    if (!refreshToken) {
      return res.status(400).json({ error: 'Refresh token required', code: 'TOKEN_REQUIRED' });
    }

    const refreshSecret = process.env.JWT_REFRESH_SECRET || 'arenafall-refresh-secret';
    const decoded = jwt.verify(refreshToken, refreshSecret);

    const player = await Player.findOne({ playerId: decoded.playerId });
    if (!player) {
      return res.status(401).json({ error: 'Player not found', code: 'PLAYER_NOT_FOUND' });
    }

    const tokens = generateTokens(player);
    res.json({ message: 'Token refreshed', ...tokens });
  } catch (err) {
    if (err.name === 'TokenExpiredError') {
      return res.status(401).json({ error: 'Refresh token expired', code: 'REFRESH_EXPIRED' });
    }
    return res.status(401).json({ error: 'Invalid refresh token', code: 'INVALID_TOKEN' });
  }
});

// ─── GET /me ────────────────────────────────────────────────────
router.get('/me', authenticate, async (req, res) => {
  try {
    const player = await Player.findOne({ playerId: req.player.playerId });
    if (!player) {
      return res.status(404).json({ error: 'Player not found', code: 'NOT_FOUND' });
    }
    res.json({ player: sanitizePlayer(player) });
  } catch (err) {
    res.status(500).json({ error: 'Failed to get profile', code: 'SERVER_ERROR' });
  }
});

// ─── PUT /settings ──────────────────────────────────────────────
router.put('/settings', authenticate, async (req, res) => {
  try {
    const allowed = ['masterVolume', 'musicVolume', 'sfxVolume', 'sensitivity', 'invertY', 'colorblindMode', 'crosshair'];
    const updates = {};
    
    for (const key of allowed) {
      if (req.body[key] !== undefined) {
        updates[`settings.${key}`] = req.body[key];
      }
    }

    const player = await Player.findOneAndUpdate(
      { playerId: req.player.playerId },
      { $set: updates },
      { new: true }
    );

    if (!player) {
      return res.status(404).json({ error: 'Player not found', code: 'NOT_FOUND' });
    }

    res.json({ message: 'Settings updated', settings: player.settings });
  } catch (err) {
    res.status(500).json({ error: 'Failed to update settings', code: 'SERVER_ERROR' });
  }
});

// ─── POST /logout ───────────────────────────────────────────────
router.post('/logout', authenticate, (req, res) => {
  res.json({ message: 'Logged out successfully' });
});

// ─── DELETE /account ────────────────────────────────────────────
router.delete('/account', authenticate, async (req, res) => {
  try {
    await Player.findOneAndDelete({ playerId: req.player.playerId });
    logger.info(`🗑️ Account deleted: ${req.player.username}`);
    res.json({ message: 'Account deleted' });
  } catch (err) {
    res.status(500).json({ error: 'Failed to delete account', code: 'SERVER_ERROR' });
  }
});

module.exports = router;
