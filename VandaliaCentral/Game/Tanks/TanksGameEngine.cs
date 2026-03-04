using VandaliaCentral.Models.Tanks;

namespace VandaliaCentral.Game.Tanks;

public sealed class TanksGameEngine
{
    private const float TankRadius = 18f;
    private const float TankMoveSpeed = 220f;
    private const float TankTurnSpeedRad = 2.8f;
    private const float BulletRadius = 5f;
    private const float BulletSpeed = 520f;
    private const float BulletTtlSeconds = 2f;
    private const float FireCooldownSeconds = 0.25f;
    private const int MaxHp = 3;
    private const float RespawnDelaySeconds = 2f;

    private readonly object _gate = new();
    private readonly TanksMap _map;
    private readonly Dictionary<string, PlayerRuntimeState> _players = new();
    private readonly Dictionary<int, BulletRuntimeState> _bullets = new();
    private readonly Random _random = new();

    private int _nextBulletId = 1;
    private long _tick;

    public TanksGameEngine(IWebHostEnvironment env)
    {
        _map = TanksMap.LoadOrDefault(env.WebRootPath);
    }

    public int MaxPlayers { get; set; } = 12;

    public bool JoinGame(string connectionId, string name, out string? error)
    {
        lock (_gate)
        {
            error = null;
            if (_players.ContainsKey(connectionId))
            {
                return true;
            }

            if (_players.Count >= MaxPlayers)
            {
                error = "Lobby is full.";
                return false;
            }

            var spawn = PickSpawnPoint();
            _players[connectionId] = new PlayerRuntimeState
            {
                PlayerId = connectionId,
                Name = string.IsNullOrWhiteSpace(name) ? "Tank" : name.Trim(),
                X = spawn.X,
                Y = spawn.Y,
                BodyAngle = 0,
                TurretAngle = 0,
                Hp = MaxHp,
                IsAlive = true,
                LastFireTimeSeconds = -10
            };

            return true;
        }
    }

    public void LeaveGame(string connectionId)
    {
        lock (_gate)
        {
            _players.Remove(connectionId);
            var bulletsToDelete = _bullets.Values.Where(b => b.ShooterPlayerId == connectionId).Select(b => b.BulletId).ToList();
            foreach (var bulletId in bulletsToDelete)
            {
                _bullets.Remove(bulletId);
            }
        }
    }

    public void ApplyInput(string connectionId, InputDto input)
    {
        lock (_gate)
        {
            if (_players.TryGetValue(connectionId, out var player))
            {
                player.Input = input;
            }
        }
    }

    public void Tick(float dtSeconds)
    {
        lock (_gate)
        {
            _tick++;
            var nowSeconds = _tick * dtSeconds;

            foreach (var player in _players.Values)
            {
                TickPlayer(player, dtSeconds, nowSeconds);
            }

            TickBullets(dtSeconds, nowSeconds);
        }
    }

    public SnapshotDto CreateSnapshot()
    {
        lock (_gate)
        {
            return new SnapshotDto
            {
                Tick = _tick,
                ArenaWidth = _map.Width,
                ArenaHeight = _map.Height,
                Players = _players.Values.Select(p => new PlayerStateDto
                {
                    PlayerId = p.PlayerId,
                    Name = p.Name,
                    X = p.X,
                    Y = p.Y,
                    BodyAngle = p.BodyAngle,
                    TurretAngle = p.TurretAngle,
                    Hp = p.Hp,
                    IsAlive = p.IsAlive,
                    Kills = p.Kills,
                    Deaths = p.Deaths
                }).ToList(),
                Bullets = _bullets.Values.Select(b => new BulletStateDto
                {
                    BulletId = b.BulletId,
                    ShooterPlayerId = b.ShooterPlayerId,
                    X = b.X,
                    Y = b.Y,
                    Radius = BulletRadius
                }).ToList(),
                Obstacles = _map.Obstacles.Select(o => new ObstacleDto
                {
                    X = o.X,
                    Y = o.Y,
                    Width = o.Width,
                    Height = o.Height
                }).ToList()
            };
        }
    }

    public LobbyStateDto CreateLobbyState()
    {
        lock (_gate)
        {
            return new LobbyStateDto
            {
                MaxPlayers = MaxPlayers,
                Players = _players.Values.Select(p => new LobbyPlayerDto
                {
                    PlayerId = p.PlayerId,
                    Name = p.Name,
                    Kills = p.Kills,
                    Deaths = p.Deaths
                }).OrderByDescending(p => p.Kills).ThenBy(p => p.Deaths).ToList()
            };
        }
    }

    private void TickPlayer(PlayerRuntimeState player, float dtSeconds, float nowSeconds)
    {
        if (!player.IsAlive)
        {
            if (nowSeconds >= player.RespawnAtSeconds)
            {
                Respawn(player);
            }
            return;
        }

        if (player.Input.TurnLeft)
        {
            player.BodyAngle -= TankTurnSpeedRad * dtSeconds;
        }

        if (player.Input.TurnRight)
        {
            player.BodyAngle += TankTurnSpeedRad * dtSeconds;
        }

        var moveAxis = 0f;
        if (player.Input.Forward)
        {
            moveAxis += 1;
        }

        if (player.Input.Backward)
        {
            moveAxis -= 1;
        }

        var moveDistance = moveAxis * TankMoveSpeed * dtSeconds;
        if (Math.Abs(moveDistance) > 0.01f)
        {
            var dirX = MathF.Cos(player.BodyAngle);
            var dirY = MathF.Sin(player.BodyAngle);
            MoveWithCollision(player, dirX * moveDistance, dirY * moveDistance);
        }

        player.TurretAngle = MathF.Atan2(player.Input.MouseY - player.Y, player.Input.MouseX - player.X);

        if (player.Input.FirePressed && nowSeconds - player.LastFireTimeSeconds >= FireCooldownSeconds)
        {
            SpawnBullet(player, nowSeconds);
            player.LastFireTimeSeconds = nowSeconds;
        }
    }

    private void TickBullets(float dtSeconds, float nowSeconds)
    {
        var deleteBulletIds = new List<int>();

        foreach (var bullet in _bullets.Values)
        {
            bullet.X += bullet.VelocityX * dtSeconds;
            bullet.Y += bullet.VelocityY * dtSeconds;

            if (nowSeconds >= bullet.ExpiresAtSeconds)
            {
                deleteBulletIds.Add(bullet.BulletId);
                continue;
            }

            if (IntersectsWorldOrObstacles(bullet.X, bullet.Y, BulletRadius))
            {
                deleteBulletIds.Add(bullet.BulletId);
                continue;
            }

            foreach (var target in _players.Values)
            {
                if (!target.IsAlive || target.PlayerId == bullet.ShooterPlayerId)
                {
                    continue;
                }

                var dx = target.X - bullet.X;
                var dy = target.Y - bullet.Y;
                var combinedRadius = TankRadius + BulletRadius;
                if (dx * dx + dy * dy <= combinedRadius * combinedRadius)
                {
                    target.Hp -= 1;
                    if (target.Hp <= 0)
                    {
                        KillPlayer(target, bullet.ShooterPlayerId, nowSeconds);
                    }

                    deleteBulletIds.Add(bullet.BulletId);
                    break;
                }
            }
        }

        foreach (var bulletId in deleteBulletIds.Distinct())
        {
            _bullets.Remove(bulletId);
        }
    }

    private void KillPlayer(PlayerRuntimeState target, string shooterId, float nowSeconds)
    {
        target.IsAlive = false;
        target.Deaths += 1;
        target.RespawnAtSeconds = nowSeconds + RespawnDelaySeconds;

        if (_players.TryGetValue(shooterId, out var shooter) && shooter.PlayerId != target.PlayerId)
        {
            shooter.Kills += 1;
        }
    }

    private void Respawn(PlayerRuntimeState player)
    {
        var spawn = PickSpawnPoint();
        player.X = spawn.X;
        player.Y = spawn.Y;
        player.Hp = MaxHp;
        player.IsAlive = true;
        player.BodyAngle = 0;
        player.TurretAngle = 0;
    }

    private void SpawnBullet(PlayerRuntimeState player, float nowSeconds)
    {
        var bulletId = _nextBulletId++;
        var spawnX = player.X + MathF.Cos(player.TurretAngle) * (TankRadius + 8);
        var spawnY = player.Y + MathF.Sin(player.TurretAngle) * (TankRadius + 8);

        _bullets[bulletId] = new BulletRuntimeState
        {
            BulletId = bulletId,
            ShooterPlayerId = player.PlayerId,
            X = spawnX,
            Y = spawnY,
            VelocityX = MathF.Cos(player.TurretAngle) * BulletSpeed,
            VelocityY = MathF.Sin(player.TurretAngle) * BulletSpeed,
            ExpiresAtSeconds = nowSeconds + BulletTtlSeconds
        };
    }

    private void MoveWithCollision(PlayerRuntimeState player, float dx, float dy)
    {
        var newX = player.X + dx;
        if (!IntersectsWorldOrObstacles(newX, player.Y, TankRadius))
        {
            player.X = newX;
        }

        var newY = player.Y + dy;
        if (!IntersectsWorldOrObstacles(player.X, newY, TankRadius))
        {
            player.Y = newY;
        }
    }

    private bool IntersectsWorldOrObstacles(float x, float y, float radius)
    {
        if (x - radius < 0 || x + radius > _map.Width || y - radius < 0 || y + radius > _map.Height)
        {
            return true;
        }

        foreach (var obstacle in _map.Obstacles)
        {
            var closestX = Math.Clamp(x, obstacle.X, obstacle.X + obstacle.Width);
            var closestY = Math.Clamp(y, obstacle.Y, obstacle.Y + obstacle.Height);
            var dx = x - closestX;
            var dy = y - closestY;
            if (dx * dx + dy * dy <= radius * radius)
            {
                return true;
            }
        }

        return false;
    }

    private SpawnPoint PickSpawnPoint()
    {
        var idx = _random.Next(_map.SpawnPoints.Count);
        return _map.SpawnPoints[idx];
    }

    private sealed class PlayerRuntimeState
    {
        public string PlayerId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public float X { get; set; }
        public float Y { get; set; }
        public float BodyAngle { get; set; }
        public float TurretAngle { get; set; }
        public int Hp { get; set; }
        public bool IsAlive { get; set; }
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public float LastFireTimeSeconds { get; set; }
        public float RespawnAtSeconds { get; set; }
        public InputDto Input { get; set; } = new();
    }

    private sealed class BulletRuntimeState
    {
        public int BulletId { get; set; }
        public string ShooterPlayerId { get; set; } = string.Empty;
        public float X { get; set; }
        public float Y { get; set; }
        public float VelocityX { get; set; }
        public float VelocityY { get; set; }
        public float ExpiresAtSeconds { get; set; }
    }
}
