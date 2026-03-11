using System.Collections.Generic;

public class RegisterSystems
{
    public static List<ISystem> GetListOfSystems()
    {
        var toRegister = new List<ISystem>();
        toRegister.Add(new SpawnSystem());
        toRegister.Add(new RewindSystem());
        toRegister.Add(new MovementSystem());
        toRegister.Add(new WallBounceSystem());
        toRegister.Add(new CircleCollisionSystem());
        toRegister.Add(new ProtectionSystem());
        toRegister.Add(new ExplosionSystem());
        toRegister.Add(new DestroyWhenSizeZeroSystem());
        toRegister.Add(new ColorSystem());



        return toRegister;
    }
}