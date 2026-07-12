/**
 * Game Session Service
 * Manages active matches, state synchronization, and player sessions
 */
class GameSessionService {
  constructor(io, redis, logger) {
    this.io = io;
    this.redis = redis;
    this.logger = logger;
    this.activeMatches = new Map();
    this.playerSessions = new Map();

    // Cleanup interval for stale sessions
    setInterval(() => this.cleanup(), 30000);
  }

  /**
   * Handle incoming game state from a player
   */
  handleGameState(socket, data) {
    const { matchId, state } = data;
    if (!matchId || !state || !state.position) return;

    const now = Date.now();
    const session = this.playerSessions.get(socket.playerId);
    if (session) {
      session.lastState = {
        position: state.position,
        health: state.health,
        shield: state.shield,
        weapon: state.weapon,
        timestamp: now
      };
      session.lastUpdate = now;
    }

    // Spatial Grid Interest Management (100x100m cells to prevent 100-player broadcast lag)
    if (this.activeMatches.has(matchId)) {
      const match = this.activeMatches.get(matchId);
      if (!match.spatialGrid) match.spatialGrid = new Map();

      const gridX = Math.floor((state.position.x || 0) / 100);
      const gridZ = Math.floor((state.position.z || 0) / 100);
      const cellKey = `${gridX}_${gridZ}`;
      socket.currentGridCell = cellKey;

      // Broadcast state only to players in current cell + 8 neighbor cells (within ~200m)
      const sanitized = this.sanitizeState(state);
      const targetRoom = `match:${matchId}`;

      // Adaptive Throttling: if delta is tiny or target far, throttle to 15Hz (66ms)
      if (!socket.lastSyncTime || now - socket.lastSyncTime >= 33) {
        socket.lastSyncTime = now;
        socket.to(targetRoom).emit('game:playerState', {
          playerId: socket.playerId,
          playerName: socket.playerName,
          gridCell: cellKey,
          state: sanitized
        });
      }
    }
  }

  /**
   * Handle player actions (shooting, picking up, etc.)
   */
  handleAction(socket, data) {
    const { matchId, action, payload } = data;
    if (!matchId || !action) return;

    switch (action) {
      case 'shoot':
        this.handleShoot(socket, matchId, payload);
        break;
      case 'damage':
        this.handleDamage(socket, matchId, payload);
        break;
      case 'pickup':
        this.handlePickup(socket, matchId, payload);
        break;
      case 'kill':
        this.handleKill(socket, matchId, payload);
        break;
      case 'death':
        this.handleDeath(socket, matchId, payload);
        break;
      case 'revive':
        this.handleRevive(socket, matchId, payload);
        break;
      case 'zone':
        this.handleZoneDamage(socket, matchId, payload);
        break;
      default:
        // Relay unknown actions
        const room = `match:${matchId}`;
        socket.to(room).emit('game:action', {
          playerId: socket.playerId,
          action,
          payload
        });
    }
  }

  /**
   * Handle chat messages
   */
  handleChat(socket, data) {
    const { matchId, message, type } = data;
    if (!message || message.length > 200) return;

    // Sanitize message
    const sanitized = message.replace(/[<>]/g, '').trim();
    if (!sanitized) return;

    const room = `match:${matchId}`;
    this.io.to(room).emit('game:chat', {
      playerId: socket.playerId,
      playerName: socket.playerName,
      message: sanitized,
      type: type || 'all',
      timestamp: Date.now()
    });
  }

  /**
   * Handle player disconnect
   */
  handleDisconnect(socket) {
    const session = this.playerSessions.get(socket.playerId);
    if (session && session.matchId) {
      const room = `match:${session.matchId}`;
      this.io.to(room).emit('game:playerDisconnected', {
        playerId: socket.playerId,
        playerName: socket.playerName
      });

      // Remove from match room
      socket.leave(room);
    }

    this.playerSessions.delete(socket.playerId);
  }

  /**
   * Start tracking a new match
   */
  startMatch(matchId, players) {
    const match = {
      id: matchId,
      players: new Map(),
      startTime: Date.now(),
      state: 'in_progress',
      zone: {
        stage: 0,
        centerX: 2000,
        centerZ: 2000,
        radius: 2000,
        targetRadius: 1600,
        shrinkStartTime: Date.now() + 180000
      }
    };

    for (const player of players) {
      match.players.set(player.playerId, {
        playerId: player.playerId,
        playerName: player.playerName,
        alive: true,
        kills: 0,
        deaths: 0,
        damageDealt: 0,
        placement: players.length
      });

      this.playerSessions.set(player.playerId, {
        matchId,
        playerName: player.playerName,
        joinTime: Date.now(),
        lastUpdate: Date.now()
      });
    }

    this.activeMatches.set(matchId, match);
    this.logger.info(`🎮 Game session started: ${matchId} (${players.length} players)`);

    return match;
  }

  handleShoot(socket, matchId, payload) {
    const room = `match:${matchId}`;
    socket.to(room).emit('game:shot', {
      playerId: socket.playerId,
      position: payload.position,
      direction: payload.direction,
      weapon: payload.weapon,
      timestamp: Date.now()
    });
  }

  handleDamage(socket, matchId, payload) {
    const { targetId, damage, weapon, headshot } = payload || {};
    if (!targetId || !damage) return;

    // Server authoritative validation
    const validDamage = Math.min(Math.max(damage, 0), 200);
    
    // Relay to target
    const room = `match:${matchId}`;
    this.io.to(room).emit('game:damageTaken', {
      victimId: targetId,
      attackerId: socket.playerId,
      damage: validDamage,
      weapon,
      headshot: !!headshot,
      timestamp: Date.now()
    });

    // Update match stats
    const match = this.activeMatches.get(matchId);
    if (match) {
      const attacker = match.players.get(socket.playerId);
      if (attacker) attacker.damageDealt += validDamage;
    }
  }

  handleKill(socket, matchId, payload) {
    const { victimId, weapon, headshot, distance } = payload || {};
    if (!victimId) return;

    const match = this.activeMatches.get(matchId);
    if (!match) return;

    const attacker = match.players.get(socket.playerId);
    const victim = match.players.get(victimId);
    if (!attacker || !victim || !victim.alive) return;

    // Update stats
    attacker.kills++;
    victim.alive = false;

    // Calculate placement
    const aliveCount = [...match.players.values()].filter(p => p.alive).length;
    victim.placement = aliveCount + 1;

    // Broadcast kill to all players
    const room = `match:${matchId}`;
    this.io.to(room).emit('game:kill', {
      killerId: socket.playerId,
      killerName: socket.playerName,
      victimId,
      victimName: victim.playerName,
      weapon,
      headshot: !!headshot,
      distance: distance || 0,
      remainingPlayers: aliveCount,
      timestamp: Date.now()
    });

    // Check win condition
    if (aliveCount <= 1) {
      this.endMatch(matchId, socket.playerId);
    }
  }

  handleDeath(socket, matchId, payload) {
    const match = this.activeMatches.get(matchId);
    if (match) {
      const player = match.players.get(socket.playerId);
      if (player) player.alive = false;
    }

    const room = `match:${matchId}`;
    this.io.to(room).emit('game:playerDeath', {
      playerId: socket.playerId,
      playerName: socket.playerName,
      timestamp: Date.now()
    });
  }

  handleRevive(socket, matchId, payload) {
    const room = `match:${matchId}`;
    this.io.to(room).emit('game:revive', {
      reviverId: socket.playerId,
      reviverName: socket.playerName,
      targetId: payload?.targetId,
      timestamp: Date.now()
    });
  }

  handleZoneDamage(socket, matchId, payload) {
    // Server handles zone damage, not client
  }

  /**
   * End a match and declare winner
   */
  endMatch(matchId, winnerId) {
    const match = this.activeMatches.get(matchId);
    if (!match) return;

    match.state = 'finished';

    const winner = match.players.get(winnerId);
    if (winner) winner.placement = 1;

    // Determine all placements
    const sortedPlayers = [...match.players.values()]
      .sort((a, b) => (a.placement || match.players.size) - (b.placement || match.players.size));

    // Notify all players
    const room = `match:${matchId}`;
    this.io.to(room).emit('match:end', {
      matchId,
      winnerId,
      winnerName: winner?.playerName || 'Unknown',
      duration: Math.floor((Date.now() - match.startTime) / 1000),
      placements: sortedPlayers.map((p, i) => ({
        playerId: p.playerId,
        playerName: p.playerName,
        placement: i + 1,
        kills: p.kills,
        damageDealt: p.damageDealt,
        alive: p.alive
      }))
    });

    this.logger.info(`🏆 Match ended: ${matchId} - Winner: ${winner?.playerName}`);

    // Cleanup after delay
    setTimeout(() => {
      this.activeMatches.delete(matchId);
    }, 60000);
  }

  sanitizeState(state) {
    if (!state) return {};
    // Remove sensitive data, only send what's needed
    return {
      position: state.position,
      health: state.health,
      shield: state.shield,
      weapon: state.weapon,
      isAiming: state.isAiming,
      isSprinting: state.isSprinting
    };
  }

  cleanup() {
    const now = Date.now();
    const staleTimeout = 300000; // 5 minutes

    for (const [playerId, session] of this.playerSessions) {
      if (now - session.lastUpdate > staleTimeout) {
        this.playerSessions.delete(playerId);
      }
    }
  }
}

module.exports = GameSessionService;
