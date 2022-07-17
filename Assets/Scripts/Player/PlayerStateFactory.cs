using System.Collections.Generic;

enum PlayerStates
{
    Idle,
    Run,
    Fall,
    Jump,
    Grounded
}

public class PlayerStateFactory
{
    PlayerStateMachine _context;
    Dictionary<PlayerStates, PlayerBaseState> _states = new Dictionary<PlayerStates, PlayerBaseState>();

    public PlayerStateFactory(PlayerStateMachine currentContext)
    {
        _context = currentContext;
        _states[PlayerStates.Idle] = new PlayerIdleState(_context, this);
        _states[PlayerStates.Run] = new PlayerRunState(_context, this);
        _states[PlayerStates.Grounded] = new PlayerGroundedState(_context, this);
        _states[PlayerStates.Jump] = new PlayerJumpState(_context, this);
        _states[PlayerStates.Fall] = new PlayerFallState(_context, this);
    }


    public PlayerBaseState Idle() => _states[PlayerStates.Idle];
    public PlayerBaseState Run() => _states[PlayerStates.Run];
    public PlayerBaseState Grounded() => _states[PlayerStates.Grounded];
    public PlayerBaseState Jump() => _states[PlayerStates.Jump];
    public PlayerBaseState Fall() => _states[PlayerStates.Fall];
}