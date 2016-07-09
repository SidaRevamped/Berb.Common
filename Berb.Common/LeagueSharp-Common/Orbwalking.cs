#region LICENSE

/*
 Copyright 2014 - 2015 LeagueSharp
 Orbwalking.cs is part of LeagueSharp.Common.
 
 LeagueSharp.Common is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.
 
 LeagueSharp.Common is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.
 
 You should have received a copy of the GNU General Public License
 along with LeagueSharp.Common. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

#region

using System;
using System.Collections.Generic;
using System.Linq;
using SharpDX;
using Color = System.Drawing.Color;
using EloBuddy;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK;
using LeagueSharp.SDK.Core.Utils;
using LeagueSharp.Common;
using LeagueSharp.SDK;
using LeagueSharp.SDK.Enumerations;

#endregion

/*
 * Test events and make sure they're working, fix if needed
 * Re-Add custom keys so things like Riven Burst can be added with ease
 * Test, test and test.
*/

namespace LeagueSharp.Common
{
    /// <summary>
    ///     This class offers everything related to auto-attacks and orbwalking.
    /// </summary>
    public static class Orbwalking
    {
        /// <summary>
        ///     Delegate AfterAttackEvenH
        /// </summary>
        /// <param name="unit">The unit.</param>
        /// <param name="target">The target.</param>
        public delegate void AfterAttackEvenH(AttackableUnit unit, AttackableUnit target);

        /// <summary>
        ///     Delegate BeforeAttackEvenH
        /// </summary>
        /// <param name="args">The <see cref="BeforeAttackEventArgs" /> instance containing the event data.</param>
        public delegate void BeforeAttackEvenH(BeforeAttackEventArgs args);

        /// <summary>
        ///     Delegate OnAttackEvenH
        /// </summary>
        /// <param name="unit">The unit.</param>
        /// <param name="target">The target.</param>
        public delegate void OnAttackEvenH(AttackableUnit unit, AttackableUnit target);

        /// <summary>
        ///     Delegate OnNonKillableMinionH
        /// </summary>
        /// <param name="minion">The minion.</param>
        public delegate void OnNonKillableMinionH(AttackableUnit minion);

        /// <summary>
        ///     Delegate OnTargetChangeH
        /// </summary>
        /// <param name="oldTarget">The old target.</param>
        /// <param name="newTarget">The new target.</param>
        public delegate void OnTargetChangeH(AttackableUnit oldTarget, AttackableUnit newTarget);

        /// <summary>
        ///     The orbwalking mode.
        /// </summary>
        public enum OrbwalkingMode
        {
            /// <summary>
            ///     The orbwalker will only last hit minions.
            /// </summary>
            LastHit,

            /// <summary>
            ///     The orbwalker will alternate between last hitting and auto attacking champions.
            /// </summary>
            Mixed,

            /// <summary>
            ///     The orbwalker will clear the lane of minions as fast as possible while attempting to get the last hit.
            /// </summary>
            LaneClear,

            /// <summary>
            ///     The orbwalker will only attack the target.
            /// </summary>
            Combo,

            /// <summary>
            ///     The orbwalker will only last hit minions as late as possible.
            /// </summary>
            Freeze,

            /// <summary>
            ///     The orbwalker will only move.
            /// </summary>
            CustomMode,

            /// <summary>
            ///     The orbwalker does nothing.
            /// </summary>
            None
        }

        /// <summary>
        ///     Spells that reset the attack timer.
        /// </summary>
        private static readonly string[] AttackResets =
        {
            "dariusnoxiantacticsonh", "fioraflurry", "garenq",
            "gravesmove", "hecarimrapidslash", "jaxempowertwo", "jaycehypercharge", "leonashieldofdaybreak", "luciane",
            "monkeykingdoubleattack", "mordekaisermaceofspades", "nasusq", "nautiluspiercinggaze", "netherblade",
            "gangplankqwrapper", "powerfist", "renektonpreexecute", "rengarq",
            "shyvanadoubleattack", "sivirw", "takedown", "talonnoxiandiplomacy", "trundletrollsmash", "vaynetumble",
            "vie", "volibearq", "xenzhaocombotarget", "yorickspectral", "reksaiq", "itemtitanichydracleave", "masochism",
            "illaoiw", "elisespiderw", "fiorae", "meditate", "sejuaninorthernwinds", "asheq"
        };


        /// <summary>
        ///     Spells that are not attacks even if they have the "attack" word in their name.
        /// </summary>
        private static readonly string[] NoAttacks =
        {
            "volleyattack", "volleyattackwithsound",
            "jarvanivcataclysmattack", "monkeykingdoubleattack", "shyvanadoubleattack", "shyvanadoubleattackdragon",
            "zyragraspingplantattack", "zyragraspingplantattack2", "zyragraspingplantattackfire",
            "zyragraspingplantattack2fire", "viktorpowertransfer", "sivirwattackbounce", "asheqattacknoonhit",
            "elisespiderlingbasicattack", "heimertyellowbasicattack", "heimertyellowbasicattack2",
            "heimertbluebasicattack", "annietibbersbasicattack", "annietibbersbasicattack2",
            "yorickdecayedghoulbasicattack", "yorickravenousghoulbasicattack", "yorickspectralghoulbasicattack",
            "malzaharvoidlingbasicattack", "malzaharvoidlingbasicattack2", "malzaharvoidlingbasicattack3",
            "kindredwolfbasicattack"
        };


        /// <summary>
        ///     Spells that are attacks even if they dont have the "attack" word in their name.
        /// </summary>
        private static readonly string[] Attacks =
        {
            "caitlynheadshotmissile", "frostarrow", "garenslash2",
            "kennenmegaproc", "masteryidoublestrike", "quinnwenhanced", "renektonexecute", "renektonsuperexecute",
            "rengarnewpassivebuffdash", "trundleq", "xenzhaothrust", "xenzhaothrust2", "xenzhaothrust3", "viktorqbuff", "lucianpassiveshot"
        };

        /// <summary>
        ///     Champs whose auto attacks can't be cancelled
        /// </summary>
        private static readonly string[] NoCancelChamps = { "Kalista" };

        /// <summary>
        ///     The last auto attack tick
        /// </summary>
        public static int LastAATick;

        /// <summary>
        ///     <c>true</c> if the orbwalker will attack.
        /// </summary>
        public static bool Attack = true;

        /// <summary>
        ///     <c>true</c> if the orbwalker will skip the next attack.
        /// </summary>
        public static bool DisableNextAttack;

        /// <summary>
        ///     <c>true</c> if the orbwalker will move.
        /// </summary>
        public static bool Move = true;

        /// <summary>
        ///     The tick the most recent attack command was sent.
        /// </summary>
        public static int LastAutoAttackCommandTick;

        /// <summary>
        ///     The last target
        /// </summary>
        private static AttackableUnit _lastTarget;

        /// <summary>
        ///     The player
        /// </summary>
        private static readonly AIHeroClient Player;

        /// <summary>
        ///     The delay
        /// </summary>
        private static int _delay;

        /// <summary>
        ///     The minimum distance
        /// </summary>
        private static float _minDistance = 400;

        /// <summary>
        ///     The champion name
        /// </summary>
        private static readonly string _championName;

        /// <summary>
        ///     The random
        /// </summary>
        private static readonly Random _random = new Random(DateTime.Now.Millisecond);

        private static float m_baseAttackSpeed;

        /// <summary>
        ///     Initializes static members of the <see cref="Orbwalking" /> class.
        /// </summary>
        static Orbwalking()
        {
            m_baseAttackSpeed = 1f / (ObjectManager.Player.AttackDelay * ObjectManager.Player.GetAttackSpeed());
            Player = ObjectManager.Player;
            _championName = Player.ChampionName;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpell;
            Obj_AI_Base.OnSpellCast += Obj_AI_Base_OnDoCast;
            Spellbook.OnStopCast += SpellbookOnStopCast;
            BeforeAttack += Orbwalking_BeforeAttack;
            EloBuddy.Player.OnIssueOrder += OnIssueOrder;

        }

        private static void OnIssueOrder(Obj_AI_Base sender, PlayerIssueOrderEventArgs args)
        {
            if (sender.IsMe && args.Order == GameObjectOrder.MoveTo)
            {
                LastMovementOrderTick = Core.GameTickCount + Game.Ping;
                //LastMovementOrderTick = args.TargetPosition;
            }
            if (sender.IsMe && args.Order == GameObjectOrder.AttackUnit)
            {
                LastAATick = Core.GameTickCount + Game.Ping;
                _lastTarget = args.Target as AttackableUnit;
            }
        }

        private static void Orbwalking_BeforeAttack(BeforeAttackEventArgs args)
        {
        }

        /// <summary>
        ///     This event is fired before the player auto attacks.
        /// </summary>
        public static event BeforeAttackEvenH BeforeAttack;

        /// <summary>
        ///     This event is fired when a unit is about to auto-attack another unit.
        /// </summary>
        public static event OnAttackEvenH OnAttack;

        /// <summary>
        ///     This event is fired after a unit finishes auto-attacking another unit (Only works with player for now).
        /// </summary>
        public static event AfterAttackEvenH AfterAttack;

        /// <summary>
        ///     Gets called on target changes
        /// </summary>
        public static event OnTargetChangeH OnTargetChange;

        /// <summary>
        ///     Occurs when a minion is not killable by an auto attack.
        /// </summary>
        public static event OnNonKillableMinionH OnNonKillableMinion;

        /// <summary>
        ///     Fires the before attack event.
        /// </summary>
        /// <param name="target">The target.</param>
        private static void FireBeforeAttack(AttackableUnit target)
        {
            if (BeforeAttack != null)
            {
                BeforeAttack(new BeforeAttackEventArgs { Target = target });
            }
            else
            {
                DisableNextAttack = false;
            }
        }

        /// <summary>
        ///     Fires the on attack event.
        /// </summary>
        /// <param name="unit">The unit.</param>
        /// <param name="target">The target.</param>
        private static void FireOnAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (OnAttack != null)
            {
                OnAttack(unit, target);
            }
        }

        /// <summary>
        ///     Fires the after attack event.
        /// </summary>
        /// <param name="unit">The unit.</param>
        /// <param name="target">The target.</param>
        private static void FireAfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (AfterAttack != null && target.LSIsValidTarget())
            {
                AfterAttack(unit, target);
            }
        }

        /// <summary>
        ///     Fires the on target switch event.
        /// </summary>
        /// <param name="newTarget">The new target.</param>
        private static void FireOnTargetSwitch(AttackableUnit newTarget)
        {
            if (OnTargetChange != null && (!_lastTarget.LSIsValidTarget() || _lastTarget != newTarget))
            {
                OnTargetChange(_lastTarget, newTarget);
            }
        }

        /// <summary>
        ///     Fires the on non killable minion event.
        /// </summary>
        /// <param name="minion">The minion.</param>
        private static void FireOnNonKillableMinion(AttackableUnit minion)
        {
            if (OnNonKillableMinion != null)
            {
                OnNonKillableMinion(minion);
            }
        }

        /// <summary>
        ///     Returns true if the spellname resets the attack timer.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns><c>true</c> if the specified name is an auto attack reset; otherwise, <c>false</c>.</returns>
        public static bool IsAutoAttackReset(string name)
        {
            return AttackResets.Contains(name.ToLower());
        }

        /// <summary>
        ///     Returns true if the unit is melee
        /// </summary>
        /// <param name="unit">The unit.</param>
        /// <returns><c>true</c> if the specified unit is melee; otherwise, <c>false</c>.</returns>
        public static bool IsMelee(this Obj_AI_Base unit)
        {
            return unit.CombatType == GameObjectCombatType.Melee;
        }

        /// <summary>
        ///     Returns true if the spellname is an auto-attack.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns><c>true</c> if the name is an auto attack; otherwise, <c>false</c>.</returns>
        public static bool IsAutoAttack(string name)
        {
            return (name.ToLower().Contains("attack") && !NoAttacks.Contains(name.ToLower())) ||
                   Attacks.Contains(name.ToLower());
        }

        /// <summary>
        ///     Returns the auto-attack range of local player with respect to the target.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <returns>System.Single.</returns>
        public static float GetRealAutoAttackRange(AttackableUnit target)
        {
            var result = Player.AttackRange + Player.BoundingRadius;
            if (target.LSIsValidTarget())
            {
                var aiBase = target as Obj_AI_Base;
                if (aiBase != null && Player.ChampionName == "Caitlyn")
                {
                    if (aiBase.HasBuff("caitlynyordletrapinternal"))
                    {
                        result += 650;
                    }
                }

                return result + target.BoundingRadius;
            }

            return result;
        }

        /// <summary>
        ///     Returns the auto-attack range of the target.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <returns>System.Single.</returns>
        public static float GetAttackRange(AIHeroClient target)
        {
            var result = target.AttackRange + target.BoundingRadius;
            return result;
        }

        /// <summary>
        ///     Returns true if the target is in auto-attack range.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public static bool InAutoAttackRange(AttackableUnit target)
        {
            if (!target.LSIsValidTarget())
            {
                return false;
            }
            var myRange = GetRealAutoAttackRange(target);
            return
                Vector2.DistanceSquared(
                    target is Obj_AI_Base ? ((Obj_AI_Base)target).ServerPosition.LSTo2D() : target.Position.LSTo2D(),
                    Player.ServerPosition.LSTo2D()) <= myRange * myRange;
        }

        /// <summary>
        ///     Returns player auto-attack missile speed.
        /// </summary>
        /// <returns>System.Single.</returns>
        public static float GetMyProjectileSpeed()
        {
            return IsMelee(Player) || _championName == "Azir" || _championName == "Velkoz" ||
                   _championName == "Viktor" && Player.HasBuff("ViktorPowerTransferReturn")
                ? float.MaxValue
                : Player.BasicAttack.MissileSpeed;
        }

        /// <summary>
        ///     Gets or sets the last movement order tick.
        /// </summary>
        public static int LastMovementOrderTick { get; set; }

        /// <summary>
        ///     Returns if the player's auto-attack is ready.
        /// </summary>
        /// <returns><c>true</c> if this instance can attack; otherwise, <c>false</c>.</returns>
        public static bool CanAttack()
        {
            if (LastAATick > Core.GameTickCount)
                return false;
            return EloBuddy.SDK.Orbwalker.CanAutoAttack;
        }

        public static int getSliderItem(Menu m, string item)
        {
            return m[item].Cast<Slider>().CurrentValue;
        }

        public static float GetAttackSpeed(this Obj_AI_Base unit)
        {
            return 1 / unit.AttackDelay;
        }

        /// <summary>
        ///     Returns true if moving won't cancel the auto-attack.
        /// </summary>
        /// <param name="extraWindup">The extra windup.</param>
        /// <returns><c>true</c> if this instance can move the specified extra windup; otherwise, <c>false</c>.</returns>
        public static bool CanMove(float extraWindup, bool disableMissileCheck = false)
        {
            var localExtraWindup = 0;
            if (ObjectManager.Player.ChampionName.Equals("Rengar") && (ObjectManager.Player.HasBuff("rengarqbase") || ObjectManager.Player.HasBuff("rengarqemp")))
            {
                localExtraWindup = 200;
            }

            return Player.Hero == Champion.Kalista || Core.GameTickCount + Game.Ping / 2 >= LastAATick + Player.AttackCastDelay * 1000 + extraWindup + localExtraWindup;
        }

        /// <summary>
        ///     Sets the movement delay.
        /// </summary>
        /// <param name="delay">The delay.</param>
        public static void SetMovementDelay(int delay)
        {
            _delay = delay;
        }

        /// <summary>
        ///     Sets the minimum orbwalk distance.
        /// </summary>
        /// <param name="d">The d.</param>
        public static void SetMinimumOrbwalkDistance(float d)
        {
            _minDistance = d;
        }

        /// <summary>
        ///     The random.
        /// </summary>
        private static readonly Random random = new Random(DateTime.Now.Millisecond);

        /// <summary>
        ///     Gets the tick until the orders Movement and Attack are blocked.
        /// </summary>
        public static int BlockOrdersUntilTick { get; private set; }

        public static int TotalAutoAttacks { get; set; }

        public static void AttackA(AttackableUnit target)
        {
            if (BlockOrdersUntilTick - Core.GameTickCount > 0)
            {
                return;
            }

            var gTarget = target;
            if (!gTarget.InAutoAttackRange() || gTarget == null || gTarget.IsDead || !gTarget.IsVisible)
            {
                return;
            }

            FireBeforeAttack(target);

            //Console.WriteLine("1 - Attacking");
            if (EloBuddy.Player.IssueOrder(GameObjectOrder.AttackUnit, gTarget))
            {
                LastAutoAttackCommandTick = Core.GameTickCount;
                _lastTarget = gTarget;
            }

            BlockOrdersUntilTick = Core.GameTickCount + 70 + Math.Min(60, Game.Ping);
        }

        /// <inheritdoc />
        public static void MoveA(Vector3 position)
        {
            if (BlockOrdersUntilTick - Core.GameTickCount > 0)
            {
                return;
            }

            if (!position.IsValid())
            {
                return;
            }

            if (Core.GameTickCount - LastMovementOrderTick < Orbwalker._config["delayMovement"].Cast<Slider>().CurrentValue)
            {
                return;
            }

            var monster = EntityManager.MinionsAndMonsters.GetJungleMonsters().Where(x => x.IsVisible && x.IsHPBarRendered && Player.LSDistance(x) < GetRealAutoAttackRange(Player));
            var minion = EntityManager.MinionsAndMonsters.GetLaneMinions().Where(x => x.IsVisible && x.IsHPBarRendered && Player.LSDistance(x) < GetRealAutoAttackRange(Player));

            if (Orbwalker.LimitAttackSpeed 
                && (GameObjects.Player.AttackDelay < 1 / 2.6f) 
                && ((Player.LSCountEnemiesInRange(GetRealAutoAttackRange(Player)) >= 1) || (monster == null ? false : monster.Count() >= 1) || (minion == null ? false : minion.Count() >= 1)))
            {
                return;
            }

            if (position.Distance(GameObjects.Player.Position)
                < GameObjects.Player.BoundingRadius
                + getSliderItem(Orbwalker.misc, "HoldPosRadius"))
            {
                if (GameObjects.Player.Path.Length > 0)
                {
                    EloBuddy.Player.IssueOrder(GameObjectOrder.Stop, GameObjects.Player.ServerPosition);
                    LastMovementOrderTick = Core.GameTickCount - 70;
                }

                return;
            }

            if (position.Distance(GameObjects.Player.ServerPosition) < GameObjects.Player.BoundingRadius)
            {
                position = GameObjects.Player.ServerPosition.LSExtend(
                    position,
                    GameObjects.Player.BoundingRadius + random.Next(0, 51));
            }

            var maximumDistance = getSliderItem(Orbwalker._config, "movementMaximumDistance");
            if (position.Distance(GameObjects.Player.ServerPosition) > maximumDistance)
            {
                position = GameObjects.Player.ServerPosition.LSExtend(
                    position,
                    maximumDistance + 25 - random.Next(0, 51));
            }

            if (Orbwalker._config["movementRandomize"].Cast<CheckBox>().CurrentValue
                && GameObjects.Player.Distance(position) > 350f)
            {
                var rAngle = 2D * Math.PI * random.NextDouble();
                var radius = GameObjects.Player.BoundingRadius / 2f;
                var x = (float)(position.X + (radius * Math.Cos(rAngle)));
                var y = (float)(position.Y + (radius * Math.Sin(rAngle)));
                position = new Vector3(x, y, NavMesh.GetHeightForPosition(x, y));
            }

            var angle = 0f;
            var currentPath = GameObjects.Player.GetWaypoints();
            if (currentPath.Count > 1 && currentPath.PathLength() > 100)
            {
                var movePath = GameObjects.Player.GetPath(position);
                if (movePath.Length > 1)
                {
                    var v1 = currentPath[1] - currentPath[0];
                    var v2 = movePath[1] - movePath[0];
                    angle = v1.AngleBetween(v2);
                    var distance = movePath.Last().DistanceSquared(currentPath.Last());
                    if ((angle < 10 && distance < 500 * 500) || distance < 50 * 50)
                    {
                        return;
                    }
                }
            }

            if (Core.GameTickCount - LastMovementOrderTick < 70 + Math.Min(60, Game.Ping) && angle < 60)
            {
                return;
            }

            if (angle >= 60 && Core.GameTickCount - LastMovementOrderTick < 60)
            {
                return;
            }

            if (LastMovementOrderTick > Core.GameTickCount)
                return;

            //Console.WriteLine("2 - Moving");
            EloBuddy.Player.IssueOrder(GameObjectOrder.MoveTo, position);
            LastMovementOrderTick = Core.GameTickCount;
        }

        /// <summary>
        ///     Orbwalks a target while moving to Position.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="position">The position.</param>
        /// <param name="extraWindup">The extra windup.</param>
        /// <param name="holdAreaRadius">The hold area radius.</param>
        /// <param name="useFixedDistance">if set to <c>true</c> [use fixed distance].</param>
        /// <param name="randomizeMinDistance">if set to <c>true</c> [randomize minimum distance].</param>
        public static void Orbwalk(AttackableUnit target,
            Vector3 position,
            float extraWindup = 90,
            float holdAreaRadius = 0,
            bool useFixedDistance = true,
            bool randomizeMinDistance = true)
        {

            if (CanAttack() && Attack && !GameObjects.Player.IsCastingInterruptableSpell())
            {
                var gTarget = target;
                if (gTarget.InAutoAttackRange() && gTarget != null && !gTarget.IsDead && gTarget.IsVisible)
                {
                    AttackA(gTarget);
                }
            }

            if (CanMove(extraWindup) && Move && !GameObjects.Player.IsCastingInterruptableSpell(true))
            {
                MoveA(position.IsValid() ? position : Game.CursorPos);
            }
        }

        /// <summary>
        ///     Resets the Auto-Attack timer.
        /// </summary>
        public static void ResetAutoAttackTimer()
        {
            LastAATick = 0;
        }

        /// <summary>
        ///     Fired when the spellbook stops casting a spell.
        /// </summary>
        /// <param name="spellbook">The spellbook.</param>
        /// <param name="args">The <see cref="SpellbookStopCastEventArgs" /> instance containing the event data.</param>
        private static void SpellbookOnStopCast(Obj_AI_Base sender, SpellbookStopCastEventArgs args)
        {
            if (sender.IsValid && sender.IsMe && args.DestroyMissile && args.StopAnimation)
            {
                ResetAutoAttackTimer();
            }
        }

        /// <summary>
        ///     Fired when an auto attack is fired.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="GameObjectProcessSpellCastEventArgs" /> instance containing the event data.</param>
        private static void Obj_AI_Base_OnDoCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                var ping = Game.Ping;
                if (ping <= 30) //First world problems kappa
                {
                    Utility.DelayAction.Add(30 - ping, () => Obj_AI_Base_OnDoCast_Delayed(sender, args));
                    return;
                }

                Obj_AI_Base_OnDoCast_Delayed(sender, args);
            }
        }

        /// <summary>
        ///     Fired 30ms after an auto attack is launched.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="GameObjectProcessSpellCastEventArgs" /> instance containing the event data.</param>
        private static void Obj_AI_Base_OnDoCast_Delayed(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (IsAutoAttackReset(args.SData.Name))
            {
                ResetAutoAttackTimer();
            }

            if (IsAutoAttack(args.SData.Name))
            {
                FireAfterAttack(sender, args.Target as AttackableUnit);
            }
        }

        /// <summary>
        ///     Handles the <see cref="E:ProcessSpell" /> event.
        /// </summary>
        /// <param name="unit">The unit.</param>
        /// <param name="Spell">The <see cref="GameObjectProcessSpellCastEventArgs" /> instance containing the event data.</param>
        private static void OnProcessSpell(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs Spell)
        {
            try
            {
                var spellName = Spell.SData.Name;

                if (unit.IsMe && IsAutoAttackReset(spellName) && Spell.SData.SpellCastTime == 0)
                {
                    ResetAutoAttackTimer();
                }

                if (!IsAutoAttack(spellName))
                {
                    return;
                }

                if (unit.IsMe && (Spell.Target is Obj_AI_Base || Spell.Target is Obj_BarracksDampener || Spell.Target is Obj_HQ))
                {
                    LastAATick = Utils.GameTimeTickCount - Game.Ping / 2;
                    LastMovementOrderTick = 0;
                    TotalAutoAttacks++;

                    if (Spell.Target is Obj_AI_Base)
                    {
                        var target = (Obj_AI_Base)Spell.Target;
                        if (target.IsValid)
                        {
                            FireOnTargetSwitch(target);
                            _lastTarget = target;
                        }
                    }
                }

                FireOnAttack(unit, _lastTarget);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        /// <summary>
        ///     The before attack event arguments.
        /// </summary>
        public class BeforeAttackEventArgs : EventArgs
        {
            /// <summary>
            ///     <c>true</c> if the orbwalker should continue with the attack.
            /// </summary>
            private bool _process = true;

            /// <summary>
            ///     The target
            /// </summary>
            public AttackableUnit Target;

            /// <summary>
            ///     The unit
            /// </summary>
            public Obj_AI_Base Unit = ObjectManager.Player;

            /// <summary>
            ///     Gets or sets a value indicating whether this <see cref="BeforeAttackEventArgs" /> should continue with the attack.
            /// </summary>
            /// <value><c>true</c> if the orbwalker should continue with the attack; otherwise, <c>false</c>.</value>
            public bool Process
            {
                get { return _process; }
                set
                {
                    DisableNextAttack = !value;
                    _process = value;
                }
            }
        }

        /// <summary>
        ///     This class allows you to add an instance of "Orbwalker" to your assembly in order to control the orbwalking in an
        ///     easy way.
        /// </summary>
        public class Orbwalker
        {
            /// <summary>
            ///     The lane clear wait time modifier.
            /// </summary>
            private const float LaneClearWaitTimeMod = 2f;

            /// <summary>
            ///     The configuration
            /// </summary>
            public static Menu _config;

            /// <summary>
            ///     The instances of the orbwalker.
            /// </summary>
            public static List<Orbwalker> Instances = new List<Orbwalker>();

            /// <summary>
            ///     The player
            /// </summary>
            private readonly AIHeroClient Player;

            /// <summary>
            ///     The forced target
            /// </summary>
            private Obj_AI_Base _forcedTarget;

            /// <summary>
            ///     The orbalker mode
            /// </summary>
            private OrbwalkingMode _mode = OrbwalkingMode.None;

            /// <summary>
            ///     The orbwalking point
            /// </summary>
            private Vector3 _orbwalkingPoint;

            /// <summary>
            ///     The previous minion the orbwalker was targeting.
            /// </summary>
            private Obj_AI_Minion _prevMinion;

            /// <summary>
            ///     The name of the CustomMode if it is set.
            /// </summary>
            private string CustomModeName;

            public static Menu drawings, misc;

            /// <summary>
            ///     Initializes a new instance of the <see cref="Orbwalker" /> class.
            /// </summary>
            /// <param name="attachToMenu">The menu the orbwalker should attach to.</param>
            public Orbwalker()
            {
                _config = MainMenu.AddMenu("L# Orbwalker", "LSOrbwalker");
                _config.AddGroupLabel("Keys : ");
                _config.Add("LastHit", new KeyBind("Last hit", false, KeyBind.BindTypes.HoldActive, 'X'));
                _config.Add("Farm", new KeyBind("Mixed", false, KeyBind.BindTypes.HoldActive, 'C'));
                _config.Add("Freeze", new KeyBind("Freeze", false, KeyBind.BindTypes.HoldActive, 'N'));
                _config.Add("LaneClear", new KeyBind("LaneClear", false, KeyBind.BindTypes.HoldActive, 'V'));
                _config.Add("Orbwalk", new KeyBind("Combo", false, KeyBind.BindTypes.HoldActive, 32));
                _config.Add("StillCombo", new KeyBind("Combo without moving", false, KeyBind.BindTypes.HoldActive, 'N'));
                _config.AddGroupLabel("Extra : ");
                _config.Add("movementRandomize", new CheckBox("Randomize Location"));
                _config.Add("ExtraWindup", new Slider("Extra windup time", 80, 0, 200));
                _config.Add("FarmDelay", new Slider("Farm delay", 0, 0, 200));
                _config.Add("delayMovement", new Slider("Movement delay", 0, 0, 500));
                _config.Add("movementMaximumDistance", new Slider("Maximum Distance", 1500, 500, 1500));


                /* Drawings submenu */
                drawings = _config.AddSubMenu("Drawings", "drawings");
                drawings.Add("AACircle", new CheckBox("AACircle", true));//.SetValue(new Circle(true, Color.FromArgb(155, 255, 255, 0))));
                drawings.Add("AACircle2", new CheckBox("Enemy AA circle", false));//.SetValue(new Circle(false, Color.FromArgb(155, 255, 255, 0))));
                drawings.Add("HoldZone", new CheckBox("HoldZone", false));//.SetValue(new Circle(false, Color.FromArgb(155, 255, 255, 0))));
                drawings.Add("AALineWidth", new Slider("Line Width", 2, 1, 6));
                drawings.Add("LastHitHelper", new CheckBox("Last Hit Helper", false));

                /* Misc options */
                misc = _config.AddSubMenu("Misc", "Misc");
                misc.Add("HoldPosRadius", new Slider("Hold Position Radius", 50, 50, 250));
                misc.Add("PriorizeFarm", new CheckBox("Priorize farm over harass", true));
                misc.Add("AttackWards", new CheckBox("Auto attack wards", false));
                misc.Add("AttackPetsnTraps", new CheckBox("Auto attack pets & traps", true));
                misc.Add("AttackBarrel", new CheckBox("Auto attack gangplank barrel", true));
                misc.Add("Smallminionsprio", new CheckBox("Jungle clear small first", false));
                misc.Add("LimitAttackSpeed", new CheckBox("Don't kite if Attack Speed > 2.5", false));
                misc.Add("FocusMinionsOverTurrets", new KeyBind("Focus minions over objectives", false, KeyBind.BindTypes.PressToggle, 'M'));
                m_baseAttackSpeed = 1f / (ObjectManager.Player.AttackDelay * ObjectManager.Player.GetAttackSpeed());

                _config["StillCombo"].Cast<KeyBind>().OnValueChange += (sender, args) => { Move = !args.NewValue; };
                Player = ObjectManager.Player;
                Game.OnUpdate += GameOnOnGameUpdate;
                Drawing.OnDraw += DrawingOnOnDraw;
                GameObject.OnCreate += Obj_SpellMissile_OnCreate;
                Instances.Add(this);
            }

            private static void Obj_SpellMissile_OnCreate(GameObject sender, EventArgs args)
            {
                if (sender.IsMe)
                {
                    var obj = (AIHeroClient)sender;
                    if (obj.IsMelee())
                        return;
                }
                if (!(sender is MissileClient) || !sender.IsValid)
                    return;
                var missile = (MissileClient)sender;
                if (missile.SpellCaster is AIHeroClient && missile.SpellCaster.IsValid && IsAutoAttack(missile.SData.Name))
                {
                    FireAfterAttack(missile.SpellCaster, _lastTarget);
                }
            }

            public static bool getCheckBoxItem(Menu m, string item)
            {
                return m[item].Cast<CheckBox>().CurrentValue;
            }

            public static int getSliderItem(Menu m, string item)
            {
                return m[item].Cast<Slider>().CurrentValue;
            }

            public static bool getKeyBindItem(Menu m, string item)
            {
                return m[item].Cast<KeyBind>().CurrentValue;
            }

            public static int getBoxItem(Menu m, string item)
            {
                return m[item].Cast<ComboBox>().CurrentValue;
            }

            /// <summary>
            ///     Gets the farm delay.
            /// </summary>
            /// <value>The farm delay.</value>
            private int FarmDelay
            {
                get { return getSliderItem(_config, "FarmDelay"); }
            }

            public static bool LimitAttackSpeed
            {
                get { return getCheckBoxItem(misc, "LimitAttackSpeed"); }
            }

            /// <summary>
            ///     Gets or sets the active mode.
            /// </summary>
            /// <value>The active mode.</value>
            public OrbwalkingMode ActiveMode
            {
                get
                {
                    if (_mode != OrbwalkingMode.None)
                    {
                        return _mode;
                    }

                    if (getKeyBindItem(_config, "Orbwalk"))
                    {
                        return OrbwalkingMode.Combo;
                    }

                    if (getKeyBindItem(_config, "StillCombo"))
                    {
                        return OrbwalkingMode.Combo;
                    }

                    if (getKeyBindItem(_config, "LaneClear"))
                    {
                        return OrbwalkingMode.LaneClear;
                    }

                    if (getKeyBindItem(_config, "Farm"))
                    {
                        return OrbwalkingMode.Mixed;
                    }

                    if (getKeyBindItem(_config, "Freeze"))
                    {
                        return OrbwalkingMode.Freeze;
                    }

                    if (getKeyBindItem(_config, "LastHit"))
                    {
                        return OrbwalkingMode.LastHit;
                    }

                    //if (_config[CustomModeName] != null)
                    //{
                    //if (getKeyBindItem(_config, CustomModeName))
                    //{
                    //return OrbwalkingMode.CustomMode;
                    //}
                    //}

                    return OrbwalkingMode.None;
                }
                set { _mode = value; }
            }

            /// <summary>
            ///     Determines if a target is in auto attack range.
            /// </summary>
            /// <param name="target">The target.</param>
            /// <returns><c>true</c> if a target is in auto attack range, <c>false</c> otherwise.</returns>
            public virtual bool InAutoAttackRange(AttackableUnit target)
            {
                return Orbwalking.InAutoAttackRange(target);
            }

            /// <summary>
            ///     Registers the Custom Mode of the Orbwalker. Useful for adding a flee mode and such.
            /// </summary>
            /// <param name="name">The name of the mode Ex. "Myassembly.FleeMode" </param>
            /// <param name="displayname">The name of the mode in the menu. Ex. Flee</param>
            /// <param name="key">The default key for this mode.</param>
            public virtual void RegisterCustomMode(string name, string displayname, uint key)
            {
                CustomModeName = name;
                if (_config[name] == null)
                {
                    _config.Add(name, new KeyBind(displayname, false, KeyBind.BindTypes.HoldActive, key));
                }
            }

            /// <summary>
            ///     Enables or disables the auto-attacks.
            /// </summary>
            /// <param name="b">if set to <c>true</c> the orbwalker will attack units.</param>
            public void SetAttack(bool b)
            {
                Attack = b;
            }

            /// <summary>
            ///     Enables or disables the movement.
            /// </summary>
            /// <param name="b">if set to <c>true</c> the orbwalker will move.</param>
            public void SetMovement(bool b)
            {
                Move = b;
            }

            /// <summary>
            ///     Forces the orbwalker to attack the set target if valid and in range.
            /// </summary>
            /// <param name="target">The target.</param>
            public void ForceTarget(Obj_AI_Base target)
            {
                _forcedTarget = target;
            }

            /// <summary>
            ///     Forces the orbwalker to move to that point while orbwalking (Game.CursorPos by default).
            /// </summary>
            /// <param name="point">The point.</param>
            public void SetOrbwalkingPoint(Vector3 point)
            {
                _orbwalkingPoint = point;
            }

            public bool ShouldWait()
            {
                return
                    ObjectManager.Get<Obj_AI_Minion>()
                        .Any(
                            minion =>
                                minion.LSIsValidTarget() && minion.Team != GameObjectTeam.Neutral &&
                                InAutoAttackRange(minion) && MinionManager.IsMinion(minion, false) &&
                                HealthPrediction.LaneClearHealthPrediction(
                                    minion, (int)(Player.AttackDelay * 1000 * LaneClearWaitTimeMod), FarmDelay) <=
                                Player.GetAutoAttackDamage(minion));
            }

            private bool ShouldWaitUnderTurret(Obj_AI_Minion noneKillableMinion)
            {
                return
                    ObjectManager.Get<Obj_AI_Minion>()
                        .Any(
                            minion =>
                                (noneKillableMinion != null ? noneKillableMinion.NetworkId != minion.NetworkId : true) &&
                                minion.LSIsValidTarget() && minion.Team != GameObjectTeam.Neutral &&
                                InAutoAttackRange(minion) && MinionManager.IsMinion(minion, false) &&
                                HealthPrediction.LaneClearHealthPrediction(
                                    minion,
                                    (int)
                                        (Player.AttackDelay * 1000 +
                                         (Player.IsMelee
                                             ? Player.AttackCastDelay * 1000
                                             : Player.AttackCastDelay * 1000 +
                                               1000 * (Player.AttackRange + 2 * Player.BoundingRadius) /
                                               Player.BasicAttack.MissileSpeed)), FarmDelay) <=
                                Player.GetAutoAttackDamage(minion));
            }

            /// <summary>
            ///     Gets the target.
            /// </summary>
            /// <returns>AttackableUnit.</returns>
            public virtual AttackableUnit GetTarget()
            {
                AttackableUnit result = null;
                var mode = ActiveMode;

                if ((mode == OrbwalkingMode.Mixed || mode == OrbwalkingMode.LaneClear) &&
                    !misc["PriorizeFarm"].Cast<CheckBox>().CurrentValue)
                {
                    var target = LSTargetSelector.GetTarget(-1, DamageType.Physical);
                    if (target != null && InAutoAttackRange(target) && target.IsVisible && target.IsHPBarRendered)
                    {
                        return target;
                    }
                }

                /*Killable Minion*/
                if (mode == OrbwalkingMode.LaneClear || mode == OrbwalkingMode.Mixed || mode == OrbwalkingMode.LastHit ||
                    mode == OrbwalkingMode.Freeze)
                {
                    var MinionList =
                        ObjectManager.Get<Obj_AI_Minion>()
                            .Where(minion => minion.LSIsValidTarget() && InAutoAttackRange(minion))
                            .OrderByDescending(minion => minion.CharData.BaseSkinName.Contains("Siege"))
                            .ThenBy(minion => minion.CharData.BaseSkinName.Contains("Super"))
                            .ThenBy(minion => minion.Health)
                            .ThenByDescending(minion => minion.MaxHealth);

                    foreach (var minion in MinionList)
                    {
                        var t = (int)(Player.AttackCastDelay * 1000) - 100 + Game.Ping / 2 +
                                1000 * (int)Math.Max(0, Player.Distance(minion) - Player.BoundingRadius) /
                                (int)GetMyProjectileSpeed();

                        if (mode == OrbwalkingMode.Freeze)
                        {
                            t += 200 + Game.Ping / 2;
                        }

                        var predHealth = HealthPrediction.GetHealthPrediction(minion, t, FarmDelay);

                        if (minion.Team != GameObjectTeam.Neutral && ShouldAttackMinion(minion))
                        {
                            var damage = Player.GetAutoAttackDamage(minion, true);
                            var killable = predHealth <= damage;

                            if (mode == OrbwalkingMode.Freeze)
                            {
                                if (minion.Health < 50 || predHealth <= 50)
                                {
                                    return minion;
                                }
                            }
                            else
                            {
                                if (predHealth <= 0)
                                {
                                    FireOnNonKillableMinion(minion);
                                }

                                if (killable)
                                {
                                    return minion;
                                }
                            }
                        }

                        if (minion.Team == GameObjectTeam.Neutral && getCheckBoxItem(misc, "AttackBarrel") &&
                            minion.CharData.BaseSkinName == "gangplankbarrel" && minion.IsHPBarRendered)
                        {
                            if (minion.Health <= 1 || minion.Health <= 2 && Player.Distance(minion) >= 300)
                            {
                                return minion;
                            }
                        }
                    }
                }

                //Forced target
                if (_forcedTarget.LSIsValidTarget() && InAutoAttackRange(_forcedTarget) && _forcedTarget.IsVisible && _forcedTarget.IsHPBarRendered)
                {
                    return _forcedTarget;
                }

                /* turrets / inhibitors / nexus */
                if (mode == OrbwalkingMode.LaneClear &&
                    (!getKeyBindItem(misc, "FocusMinionsOverTurrets") ||
                     !MinionManager.GetMinions(
                         ObjectManager.Player.Position, GetRealAutoAttackRange(ObjectManager.Player)).Any()))
                {
                    /* turrets */
                    foreach (var turret in
                        ObjectManager.Get<Obj_AI_Turret>().Where(t => t.LSIsValidTarget() && InAutoAttackRange(t)))
                    {
                        return turret;
                    }

                    /* inhibitor */
                    foreach (var turret in
                        ObjectManager.Get<Obj_BarracksDampener>().Where(t => t.LSIsValidTarget() && InAutoAttackRange(t)))
                    {
                        return turret;
                    }

                    /* nexus */
                    foreach (var nexus in
                        ObjectManager.Get<Obj_HQ>().Where(t => t.LSIsValidTarget() && InAutoAttackRange(t)))
                    {
                        return nexus;
                    }
                }

                /*Champions*/
                if (mode != OrbwalkingMode.LastHit)
                {
                    if (mode != OrbwalkingMode.LaneClear || !ShouldWait())
                    {
                        var target = LSTargetSelector.GetTarget(-1, DamageType.Physical);
                        if (target.LSIsValidTarget() && InAutoAttackRange(target) && target.IsVisible && target.IsHPBarRendered)
                        {
                            return target;
                        }
                    }
                }

                /*Jungle minions*/
                if (mode == OrbwalkingMode.LaneClear || mode == OrbwalkingMode.Mixed)
                {
                    var jminions =
                        ObjectManager.Get<Obj_AI_Minion>()
                            .Where(
                                mob =>
                                    mob.LSIsValidTarget() && mob.Team == GameObjectTeam.Neutral && InAutoAttackRange(mob) &&
                                    mob.CharData.BaseSkinName != "gangplankbarrel" && mob.Name != "WardCorpse");

                    result = misc["Smallminionsprio"].Cast<CheckBox>().CurrentValue
                        ? jminions.MinOrDefault(mob => mob.MaxHealth)
                        : jminions.MaxOrDefault(mob => mob.MaxHealth);

                    if (result != null)
                    {
                        return result;
                    }
                }

                /* UnderTurret Farming */
                if (mode == OrbwalkingMode.LaneClear || mode == OrbwalkingMode.Mixed || mode == OrbwalkingMode.LastHit ||
                    mode == OrbwalkingMode.Freeze)
                {
                    var closestTower =
                        ObjectManager.Get<Obj_AI_Turret>()
                            .MinOrDefault(t => t.IsAlly && !t.IsDead ? Player.Distance(t, true) : float.MaxValue);

                    if (closestTower != null && Player.Distance(closestTower, true) < 1500 * 1500)
                    {
                        Obj_AI_Minion farmUnderTurretMinion = null;
                        Obj_AI_Minion noneKillableMinion = null;
                        // return all the minions underturret in auto attack range
                        var minions =
                            MinionManager.GetMinions(Player.Position, Player.AttackRange + 200)
                                .Where(
                                    minion =>
                                        InAutoAttackRange(minion) && closestTower.Distance(minion, true) < 900 * 900)
                                .OrderByDescending(minion => minion.CharData.BaseSkinName.Contains("Siege"))
                                .ThenBy(minion => minion.CharData.BaseSkinName.Contains("Super"))
                                .ThenByDescending(minion => minion.MaxHealth)
                                .ThenByDescending(minion => minion.Health);
                        if (minions.Any())
                        {
                            // get the turret aggro minion
                            var turretMinion =
                                minions.FirstOrDefault(
                                    minion =>
                                        minion is Obj_AI_Minion &&
                                        HealthPrediction.HasTurretAggro(minion as Obj_AI_Minion));

                            if (turretMinion != null)
                            {
                                var hpLeftBeforeDie = 0;
                                var hpLeft = 0;
                                var turretAttackCount = 0;
                                var turretStarTick = HealthPrediction.TurretAggroStartTick(
                                    turretMinion as Obj_AI_Minion);
                                // from healthprediction (don't blame me :S)
                                var turretLandTick = turretStarTick + (int)(closestTower.AttackCastDelay * 1000) +
                                                     1000 *
                                                     Math.Max(
                                                         0,
                                                         (int)
                                                             (turretMinion.Distance(closestTower) -
                                                              closestTower.BoundingRadius)) /
                                                     (int)(closestTower.BasicAttack.MissileSpeed + 70);
                                // calculate the HP before try to balance it
                                for (float i = turretLandTick + 50;
                                    i < turretLandTick + 10 * closestTower.AttackDelay * 1000 + 50;
                                    i = i + closestTower.AttackDelay * 1000)
                                {
                                    var time = (int)i - Core.GameTickCount + Game.Ping / 2;
                                    var predHP =
                                        (int)
                                            HealthPrediction.LaneClearHealthPrediction(
                                                turretMinion, time > 0 ? time : 0);
                                    if (predHP > 0)
                                    {
                                        hpLeft = predHP;
                                        turretAttackCount += 1;
                                        continue;
                                    }
                                    hpLeftBeforeDie = hpLeft;
                                    hpLeft = 0;
                                    break;
                                }
                                // calculate the hits is needed and possibilty to balance
                                if (hpLeft == 0 && turretAttackCount != 0 && hpLeftBeforeDie != 0)
                                {
                                    var damage = (int)Player.GetAutoAttackDamage(turretMinion, true);
                                    var hits = hpLeftBeforeDie / damage;
                                    var timeBeforeDie = turretLandTick +
                                                        (turretAttackCount + 1) *
                                                        (int)(closestTower.AttackDelay * 1000) -
                                                        Core.GameTickCount;
                                    var timeUntilAttackReady = LastAATick + (int)(Player.AttackDelay * 1000) >
                                                               Core.GameTickCount + Game.Ping / 2 + 25
                                        ? LastAATick + (int)(Player.AttackDelay * 1000) -
                                          (Core.GameTickCount + Game.Ping / 2 + 25)
                                        : 0;
                                    var timeToLandAttack = Player.IsMelee
                                        ? Player.AttackCastDelay * 1000
                                        : Player.AttackCastDelay * 1000 +
                                          1000 * Math.Max(0, turretMinion.Distance(Player) - Player.BoundingRadius) /
                                          Player.BasicAttack.MissileSpeed;
                                    if (hits >= 1 &&
                                        hits * Player.AttackDelay * 1000 + timeUntilAttackReady + timeToLandAttack <
                                        timeBeforeDie)
                                    {
                                        farmUnderTurretMinion = turretMinion as Obj_AI_Minion;
                                    }
                                    else if (hits >= 1 &&
                                             hits * Player.AttackDelay * 1000 + timeUntilAttackReady + timeToLandAttack >
                                             timeBeforeDie)
                                    {
                                        noneKillableMinion = turretMinion as Obj_AI_Minion;
                                    }
                                }
                                else if (hpLeft == 0 && turretAttackCount == 0 && hpLeftBeforeDie == 0)
                                {
                                    noneKillableMinion = turretMinion as Obj_AI_Minion;
                                }
                                // should wait before attacking a minion.
                                if (ShouldWaitUnderTurret(noneKillableMinion))
                                {
                                    return null;
                                }
                                if (farmUnderTurretMinion != null)
                                {
                                    return farmUnderTurretMinion;
                                }
                                // balance other minions
                                foreach (var minion in
                                    minions.Where(
                                        x =>
                                            x.NetworkId != turretMinion.NetworkId && x is Obj_AI_Minion &&
                                            !HealthPrediction.HasMinionAggro(x as Obj_AI_Minion)))
                                {
                                    var playerDamage = (int)Player.GetAutoAttackDamage(minion);
                                    var turretDamage = (int)closestTower.GetAutoAttackDamage(minion, true);
                                    var leftHP = (int)minion.Health % turretDamage;
                                    if (leftHP > playerDamage)
                                    {
                                        return minion;
                                    }
                                }
                                // late game
                                var lastminion =
                                    minions.LastOrDefault(x => x.NetworkId != turretMinion.NetworkId && x is Obj_AI_Minion &&
                                            !HealthPrediction.HasMinionAggro(x as Obj_AI_Minion));
                                if (lastminion != null && minions.Count() >= 2)
                                {
                                    if (1f / Player.AttackDelay >= 1f &&
                                        (int)(turretAttackCount * closestTower.AttackDelay / Player.AttackDelay) *
                                        Player.GetAutoAttackDamage(lastminion) > lastminion.Health)
                                    {
                                        return lastminion;
                                    }
                                    if (minions.Count() >= 5 && 1f / Player.AttackDelay >= 1.2)
                                    {
                                        return lastminion;
                                    }
                                }
                            }
                            else
                            {
                                if (ShouldWaitUnderTurret(noneKillableMinion))
                                {
                                    return null;
                                }
                                // balance other minions
                                foreach (var minion in
                                    minions.Where(
                                        x => x is Obj_AI_Minion && !HealthPrediction.HasMinionAggro(x as Obj_AI_Minion))
                                    )
                                {
                                    if (closestTower != null)
                                    {
                                        var playerDamage = (int)Player.GetAutoAttackDamage(minion);
                                        var turretDamage = (int)closestTower.GetAutoAttackDamage(minion, true);
                                        var leftHP = (int)minion.Health % turretDamage;
                                        if (leftHP > playerDamage)
                                        {
                                            return minion;
                                        }
                                    }
                                }
                                //late game
                                var lastminion =
                                    minions
                                        .LastOrDefault(x => x is Obj_AI_Minion && !HealthPrediction.HasMinionAggro(x as Obj_AI_Minion));
                                if (lastminion != null && minions.Count() >= 2)
                                {
                                    if (minions.Count() >= 5 && 1f / Player.AttackDelay >= 1.2)
                                    {
                                        return lastminion;
                                    }
                                }
                            }
                            return null;
                        }
                    }
                }

                /*Lane Clear minions*/
                if (mode == OrbwalkingMode.LaneClear)
                {
                    if (!ShouldWait())
                    {
                        if (_prevMinion.LSIsValidTarget() && InAutoAttackRange(_prevMinion))
                        {
                            var predHealth = HealthPrediction.LaneClearHealthPrediction(
                                _prevMinion, (int)(Player.AttackDelay * 1000 * LaneClearWaitTimeMod), FarmDelay);
                            if (predHealth >= 2 * Player.GetAutoAttackDamage(_prevMinion) ||
                                Math.Abs(predHealth - _prevMinion.Health) < float.Epsilon)
                            {
                                return _prevMinion;
                            }
                        }

                        result = (from minion in
                            ObjectManager.Get<Obj_AI_Minion>()
                                .Where(
                                    minion =>
                                        minion.LSIsValidTarget() && InAutoAttackRange(minion) && ShouldAttackMinion(minion, false))
                                  let predHealth =
                                      HealthPrediction.LaneClearHealthPrediction(
                                          minion, (int)(Player.AttackDelay * 1000 * LaneClearWaitTimeMod), FarmDelay)
                                  where
                                      predHealth >= 2 * Player.GetAutoAttackDamage(minion) ||
                                      Math.Abs(predHealth - minion.Health) < float.Epsilon
                                  select minion).MaxOrDefault(
                                m => !MinionManager.IsMinion(m, true) ? float.MaxValue : m.Health);

                        if (result != null)
                        {
                            _prevMinion = (Obj_AI_Minion)result;
                        }
                    }
                }

                return result;
            }

            /// <summary>
            ///     Returns if a minion should be attacked
            /// </summary>
            /// <param name="minion">The <see cref="Obj_AI_Minion" /></param>
            /// <param name="includeBarrel">Include Gangplank Barrel</param>
            /// <returns><c>true</c> if the minion should be attacked; otherwise, <c>false</c>.</returns>
            private bool ShouldAttackMinion(Obj_AI_Minion minion, bool includeBarrel = false)
            {
                if (minion.Name == "WardCorpse" || minion.CharData.BaseSkinName == "jarvanivstandard")
                {
                    return false;
                }

                if (minion.Team == GameObjectTeam.Neutral && includeBarrel)
                {
                    return getCheckBoxItem(misc, "AttackBarrel") &&
                           minion.CharData.BaseSkinName == "gangplankbarrel" && minion.IsHPBarRendered;
                }

                if (MinionManager.IsWard(minion))
                {
                    return getCheckBoxItem(misc, "AttackWards");
                }

                return (getCheckBoxItem(misc, "AttackPetsnTraps") || MinionManager.IsMinion(minion)) &&
                       minion.CharData.BaseSkinName != "gangplankbarrel";
            }

            /// <summary>
            ///     Fired when the game is updated.
            /// </summary>
            /// <param name="args">The <see cref="EventArgs" /> instance containing the event data.</param>
            private void GameOnOnGameUpdate(EventArgs args)
            {
                try
                {
                    if (ActiveMode == OrbwalkingMode.None)
                    {
                        return;
                    }

                    if (Player.IsCastingInterruptableSpell(true))
                    {
                        return;
                    }

                    var target = GetTarget();
                    Orbwalk(target, _orbwalkingPoint.LSTo2D().IsValid() ? _orbwalkingPoint : Game.CursorPos, getSliderItem(_config, "ExtraWindup"), Math.Max(getSliderItem(misc, "HoldPosRadius"), 30));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            /// <summary>
            ///     Fired when the game is drawn.
            /// </summary>
            /// <param name="args">The <see cref="EventArgs" /> instance containing the event data.</param>
            private void DrawingOnOnDraw(EventArgs args)
            {
                if (getCheckBoxItem(drawings, "AACircle"))
                {
                    Render.Circle.DrawCircle(
                        Player.Position, GetRealAutoAttackRange(null) + 65,
                        Color.FromArgb(155, 255, 255, 0),
                        getSliderItem(drawings, "AALineWidth"));
                }
                if (getCheckBoxItem(drawings, "AACircle2"))
                {
                    foreach (var target in
                        HeroManager.Enemies.FindAll(target => target.LSIsValidTarget(1175)))
                    {
                        Render.Circle.DrawCircle(
                            target.Position, GetAttackRange(target), Color.FromArgb(155, 255, 255, 0),
                            getSliderItem(drawings, "AALineWidth"));
                    }
                }

                if (getCheckBoxItem(drawings, "HoldZone"))
                {
                    Render.Circle.DrawCircle(
                        Player.Position, getSliderItem(misc, "HoldPosRadius"),
                        Color.FromArgb(155, 255, 255, 0),
                        getSliderItem(drawings, "AALineWidth"), true);
                }

                //_config.Item("FocusMinionsOverTurrets").Permashow(_config.Item("FocusMinionsOverTurrets").GetValue<KeyBind>().Active);

                if (getCheckBoxItem(drawings, "LastHitHelper"))
                {
                    foreach (var minion in
                        ObjectManager.Get<Obj_AI_Minion>()
                            .Where(
                                x => x.Name.ToLower().Contains("minion") && x.IsHPBarRendered && x.LSIsValidTarget(1000)))
                    {
                        if (minion.Health < ObjectManager.Player.LSGetAutoAttackDamage(minion, true))
                        {
                            Render.Circle.DrawCircle(minion.Position, 50, Color.LimeGreen);
                        }
                    }
                }
            }
        }
    }
}