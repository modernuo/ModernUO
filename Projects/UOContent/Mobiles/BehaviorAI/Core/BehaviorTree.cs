using System.Collections.Generic;
using System.Linq;
using Server.Items;
using Server.Engines.Spawners;
using MoveImpl = Server.Movement.MovementImpl;

namespace Server.Mobiles.BehaviorAI
{
    public class BehaviorTree
    {
        public Behavior Root { get; private set; }
        private Dictionary<BaseCreature, Queue<BehaviorQueueEntry>> behaviorQueueCache;
        private Dictionary<BaseCreature, bool> executingCache;
        public BehaviorTree()
        {
            behaviorQueueCache = new Dictionary<BaseCreature, Queue<BehaviorQueueEntry>>();
            executingCache = new Dictionary<BaseCreature, bool>();
        }
        public bool TryAddRoot(Composite behavior)
        {
            if (Root == null)
            {
                Root = behavior;
                return true;
            }
            return false;
        }
        public void Start(BehaviorTreeContext context)
        {
            if (Root != null)
            {
                Enqueue(context, Root, ExecutionFinished);
            }
        }
        public void Stop(BehaviorTreeContext context)
        {
            getQueue(context).Clear();
        }
        public virtual void Tick(BehaviorTreeContext context)
        {
            if (!executingCache.TryGetValue(context.Mobile, out bool executing))
            {
                executing = false;
                executingCache[context.Mobile] = executing;
            }

            if (!executing)
            {
                executingCache[context.Mobile] = true;
                Queue<BehaviorQueueEntry> queue = getQueue(context);
                queue.Enqueue(null);
                while (Step(context))
                {
                }
                executingCache[context.Mobile] = false;
            }
        }
        public virtual bool Step(BehaviorTreeContext context)
        {
            Queue<BehaviorQueueEntry> queue = getQueue(context);

            BehaviorQueueEntry current = queue.Dequeue();

            if (current == null || current.Behavior == null || current.Context == null)
            {
                return false;
            }

            current.Behavior.Tick(current.Context);

            if (!current.Behavior.IsRunning(current.Context))
            {
                if (current.Observer != null)
                {
                    current.Observer(current.Context, current.Behavior.GetResult(current.Context));
                }
                return true;
            }

            queue.Enqueue(current);

            return true;
        }
        public virtual void ExecutionFinished(BehaviorTreeContext context, Result result)
        {
        }
        public void Enqueue(BehaviorTreeContext context, Behavior behavior, BehaviorObserver observer)
        {
            if (!behaviorQueueCache.TryGetValue(context.Mobile, out Queue<BehaviorQueueEntry> queue))
            {
                queue = new Queue<BehaviorQueueEntry>();
                behaviorQueueCache.Add(context.Mobile, queue);
            }

            queue.Enqueue(new BehaviorQueueEntry(context, behavior, observer));
        }
        private Queue<BehaviorQueueEntry> getQueue(BehaviorTreeContext context)
        {
            if (!behaviorQueueCache.TryGetValue(context.Mobile, out Queue<BehaviorQueueEntry> queue))
            {
                queue = new Queue<BehaviorQueueEntry>();
                behaviorQueueCache.Add(context.Mobile, queue);
            }

            return queue;
        }
        public static void WalkRandomInHome(BehaviorTreeContext context, int steps)
        {
            BaseCreature mob = context.Mobile;

            if (mob.Deleted || mob.DisallowAllMoves)
            {
                return;
            }

            if (mob.Home == Point3D.Zero)
            {
                if (mob.Spawner is RegionSpawner rs)
                {
                    Region region = rs.SpawnRegion;

                    if (mob.Region.AcceptsSpawnsFrom(region))
                    {
                        mob.WalkRegion = region;
                        WalkRandom(context, steps);
                        mob.WalkRegion = null;
                    }
                    else
                    {
                        if (region.GoLocation != Point3D.Zero && Utility.RandomDouble() > 0.5)
                        {
                            DoMove(context, mob.GetDirectionTo(region.GoLocation));
                        }
                        else
                        {
                            WalkRandom(context, steps);
                        }
                    }
                }
                else
                {
                    WalkRandom(context, steps);
                }
            }
            else
            {
                for (var i = 0; i < steps; i++)
                {
                    if (mob.RangeHome != 0)
                    {
                        var currentDistance = (int)mob.GetDistanceToSqrt(mob.Home);

                        if (currentDistance < mob.RangeHome * 2 / 3)
                        {
                            WalkRandom(context, 1);
                        }
                        else if (currentDistance > mob.RangeHome)
                        {
                            DoMove(context, mob.GetDirectionTo(mob.Home));
                        }
                        else
                        {
                            if (Utility.RandomDouble() > 0.5)
                            {
                                DoMove(context, mob.GetDirectionTo(mob.Home));
                            }
                            else
                            {
                                WalkRandom(context, 1);
                            }
                        }
                    }
                    else
                    {
                        if (mob.Location != mob.Home)
                        {
                            DoMove(context, mob.GetDirectionTo(mob.Home));
                        }
                    }
                }
            }
        }
        public static MoveResult WalkRandom(BehaviorTreeContext context, int steps, bool run = false)
        {
            BaseCreature mob = context.Mobile;

            if (mob.Deleted || mob.DisallowAllMoves)
            {
                return MoveResult.BadState;
            }

            for (var i = 0; i < steps; i++)
            {
                if (Utility.Random(8) <= 8)
                {
                    var random = Utility.Random(0, 32);

                    Direction direction;

                    switch (random)
                    {
                        case 0:
                            direction = Direction.Up;
                            break;
                        case 1:
                            direction = Direction.North;
                            break;
                        case 2:
                            direction = Direction.Left;
                            break;
                        case 3:
                            direction = Direction.West;
                            break;
                        case 5:
                            direction = Direction.Down;
                            break;
                        case 6:
                            direction = Direction.South;
                            break;
                        case 7:
                            direction = Direction.Right;
                            break;
                        case 8:
                            direction = Direction.East;
                            break;
                        default:
                            direction = mob.Direction;
                            break;
                    }

                    DoMove(context, direction, run);
                }
            }
            return MoveResult.Success;
        }
        public static MoveResult DoMove(BehaviorTreeContext context, Direction d, bool run = false)
        {
            BaseCreature mob = context.Mobile;

            if (mob.Deleted || mob.Frozen || mob.Paralyzed || mob.Spell?.IsCasting == true || mob.DisallowAllMoves)
            {
                return MoveResult.BadState;
            }

            Direction direction = d;

            if (run)
            {
                direction |= Direction.Running;
            }

            mob.Pushing = false;

            MoveImpl.IgnoreMovableImpassables = mob.CanMoveOverObstacles && !mob.CanDestroyObstacles;

            if ((mob.Direction & Direction.Mask) != (direction & Direction.Mask))
            {
                bool moved = mob.Move(direction);

                MoveImpl.IgnoreMovableImpassables = false;
                return moved ? MoveResult.Success : MoveResult.Blocked;
            }

            if (mob.Move(direction))
            {
                MoveImpl.IgnoreMovableImpassables = false;
                return MoveResult.Success;
            }

            bool wasPushing = mob.Pushing;

            bool blocked = true;

            bool canOpenDoors = mob.CanOpenDoors;
            bool canDestroyObstacles = mob.CanDestroyObstacles;

            if (canOpenDoors || canDestroyObstacles)
            {
                Map map = mob.Map;

                if (map != null)
                {
                    int x = mob.X, y = mob.Y;
                    Movement.Movement.Offset(direction, ref x, ref y);

                    int destroyables = 0;
                    List<Item> obstacles = new List<Item>();

                    var eable = map.GetItemsInRange(new Point3D(x, y, mob.Location.Z), 1);

                    foreach (var item in eable)
                    {
                        if (canOpenDoors && item is BaseDoor door && door.Z + door.ItemData.Height > mob.Z && mob.Z + 16 > door.Z)
                        {
                            if (door.X != x || door.Y != y)
                            {
                                continue;
                            }

                            if (!door.Locked || !door.UseLocks())
                            {
                                obstacles.Add(item);
                            }

                            if (!canDestroyObstacles)
                            {
                                break;
                            }
                        }
                        else if (canDestroyObstacles && item.Movable && item.ItemData.Impassable && item.Z + item.ItemData.Height > mob.Z && mob.Z + 16 > item.Z)
                        {
                            if (!mob.InRange(item.GetWorldLocation(), 1))
                            {
                                continue;
                            }

                            obstacles.Add(item);
                            ++destroyables;
                        }
                    }

                    eable.Free();

                    if (destroyables > 0)
                    {
                        Effects.PlaySound(new Point3D(x, y, mob.Z), mob.Map, 0x3B3);
                    }

                    if (obstacles.Count > 0)
                        blocked = true;

                    while (obstacles.Count > 0)
                    {
                        Item item = obstacles.First();

                        if (item is BaseDoor door)
                        {
                            mob.DebugSay("Opening door...");
                            if (!door.Open)
                            {
                                door.Use(mob);
                            }
                            obstacles.Remove(item);
                        }
                        else
                        {
                            if (item is Container container)
                            {
                                for (var i = 0; i < container.Items.Count; ++i)
                                {
                                    Item check = container.Items[i];

                                    if (check.Movable && check.ItemData.Impassable && container.Z + check.ItemData.Height > mob.Z)
                                    {
                                        obstacles.Add(check);
                                    }
                                }

                                obstacles.Remove(item);
                                container.Destroy();
                            }
                            else
                            {
                                obstacles.Remove(item);
                                item.Delete();
                            }
                        }
                    }

                    if (!blocked)
                    {
                        blocked = !mob.Move(direction);
                    }
                }
            }

            if (blocked)
            {
                int offset = Utility.RandomDouble() >= 0.6 ? 1 : -1;

                for (var i = 0; i < 2; ++i)
                {
                    mob.TurnInternal(offset);

                    if (mob.Move(mob.Direction))
                    {
                        MoveImpl.IgnoreMovableImpassables = false;
                        return MoveResult.SuccessAutoTurn;
                    }
                }

                MoveImpl.IgnoreMovableImpassables = false;
                return wasPushing ? MoveResult.BadState : MoveResult.Blocked;
            }

            MoveImpl.IgnoreMovableImpassables = false;
            return MoveResult.Success;
        }
        public static bool WalkMobileRange(BehaviorTreeContext context, BaseCreature target, int steps, bool run, int minRange, int maxRange)
        {
            BaseCreature mob = context.Mobile;

            if (mob.Deleted || mob.DisallowAllMoves)
            {
                return false;
            }

            if (target == null)
            {
                return false;
            }

            for (var i = 0; i < steps; i++)
            {
                // Get the current distance
                var currentDistance = (int)mob.GetDistanceToSqrt(target);

                if (currentDistance < minRange || currentDistance > maxRange)
                {
                    var needCloser = currentDistance > maxRange;
                    var direction = needCloser ?
                        mob.GetDirectionTo(target, run) : target.GetDirectionTo(mob, run);

                    DoMove(context, direction, run);
                }
                else
                {
                    WalkRandom(context, 2, run);
                }
                return true;
            }

            // Get the current distance
            var newDistance = (int)mob.GetDistanceToSqrt(target);

            return newDistance >= minRange && newDistance <= maxRange;
        }
    }
}
