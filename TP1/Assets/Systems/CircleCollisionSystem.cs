using System.Collections.Generic;
using UnityEngine;

public sealed class CircleCollisionSystem : ISystem {
    public string Name => nameof(CircleCollisionSystem);

    public void UpdateSystem() {
        var world = GameWorld.Instance;
        var config = ECSController.Instance.Config;
        int explosionSize = config.explosionSize;

        var ids = new List<uint>();
        foreach (var id in world.Query<PositionComponent, SizeComponent>())
            ids.Add(id);

        for (int i = 0; i < ids.Count; i++) {
            uint a = ids[i];
            if (!world.TryGet(a, out PositionComponent posA) || !world.TryGet(a, out SizeComponent sizeA))
                continue;

            for (int j = i + 1; j < ids.Count; j++) {
                uint b = ids[j];
                if (!world.TryGet(b, out PositionComponent posB) || !world.TryGet(b, out SizeComponent sizeB))
                    continue;

                float rA = sizeA.Value * 0.5f;
                float rB = sizeB.Value * 0.5f;

                Vector2 delta = posB.Value - posA.Value;
                float minDist = rA + rB;

                if (delta.sqrMagnitude > minDist * minDist)
                    continue;

                world.TryGet(a, out VelocityComponent velAComp);
                world.TryGet(b, out VelocityComponent velBComp);

                Vector2 vA = velAComp != null ? velAComp.Value : Vector2.zero;
                Vector2 vB = velBComp != null ? velBComp.Value : Vector2.zero;

                CollisionResult res = CollisionUtility.CalculateCollision(
                    posA.Value, vA, sizeA.Value,
                    posB.Value, vB, sizeB.Value
                );

                if (res == null)
                    continue;

                posA.Value = res.position1;
                posB.Value = res.position2;
                ECSController.Instance.UpdateShapePosition(a, posA.Value);
                ECSController.Instance.UpdateShapePosition(b, posB.Value);

                if (velAComp != null) velAComp.Value = res.velocity1;
                if (velBComp != null) velBComp.Value = res.velocity2;

                bool aStatic = world.Has<StaticTag>(a);
                bool bStatic = world.Has<StaticTag>(b);

                if (!aStatic && !bStatic)
                {
                    bool aProtected = world.Has<ProtectedComponent>(a);
                    bool bProtected = world.Has<ProtectedComponent>(b);

                    if (sizeA.Value > sizeB.Value)
                    {
                        if (bProtected)
                        {
                            sizeA.Value -= 1;
                        }
                        else if (!aProtected)
                        {
                            sizeA.Value += 1;
                            sizeB.Value -= 1;
                        }
                    }
                    else if (sizeB.Value > sizeA.Value)
                    {
                        if (aProtected)
                        {
                            sizeB.Value -= 1;
                        }
                        else if (!bProtected)
                        {
                            sizeB.Value += 1;
                            sizeA.Value -= 1;
                        }
                    }

                    if (sizeA.Value < 0) sizeA.Value = 0;
                    if (sizeB.Value < 0) sizeB.Value = 0;

                    ECSController.Instance.UpdateShapeSize(a, sizeA.Value);
                    ECSController.Instance.UpdateShapeSize(b, sizeB.Value);

                    bool aShouldExplode = Mathf.Approximately(sizeA.Value, explosionSize - 1);
                    bool aHasTag = world.Has<WillExplodeTag>(a);
                    if (aShouldExplode && !aHasTag) world.Add(a, new WillExplodeTag());
                    else if (!aShouldExplode && aHasTag) world.Remove<WillExplodeTag>(a);

                    bool bShouldExplode = Mathf.Approximately(sizeB.Value, explosionSize - 1);
                    bool bHasTag = world.Has<WillExplodeTag>(b);
                    if (bShouldExplode && !bHasTag) world.Add(b, new WillExplodeTag());
                    else if (!bShouldExplode && bHasTag) world.Remove<WillExplodeTag>(b);
                }
                

 


                int protectionSize = config.protectionSize;
                float collisionsToProtect = config.protectionCollisionCount;
                float protectionDuration = config.protectionDuration;

                bool aEligible = world.TryGet(a, out EligibleForProtectionComponent eligA);
                bool bEligible = world.TryGet(b, out EligibleForProtectionComponent eligB);

                if (sizeA.Value == sizeB.Value)
                {

                    if (aEligible && !world.Has<ProtectedComponent>(a) && !world.Has<ProtectionCooldownComponent>(a))
                    {
                        eligA.SameSizeCollisionCount++;
                        if (eligA.SameSizeCollisionCount >= collisionsToProtect)
                        {
                            world.Remove<EligibleForProtectionComponent>(a);
                            world.Add(a, new ProtectedComponent { TimeLeft = protectionDuration });
                        }
                    }

                    if (bEligible && !world.Has<ProtectedComponent>(b) && !world.Has<ProtectionCooldownComponent>(b))
                    {
                        eligB.SameSizeCollisionCount++;
                        if (eligB.SameSizeCollisionCount >= collisionsToProtect)
                        {
                            world.Remove<EligibleForProtectionComponent>(b);
                            world.Add(b, new ProtectedComponent { TimeLeft = protectionDuration });
                        }
                    }
                }
            }
        }
    }
}