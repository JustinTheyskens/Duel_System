using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Server;
using Server.Misc;
using Server.Items;
using Server.Regions;
using Server.Commands;
using Server.Targeting;
using Server.Gumps;
using Ultima;

namespace Server.Mobiles
{
	
	public static class DuelInfo
	{

		public static List<Mobile> Fighters = new List<Mobile>();
        public static List<Mobile> Blacklist = new List<Mobile>(); // if a player runs away
        public static List<Mobile> OptOut = new List<Mobile>(); // if players don't wish to duel
        public static Dictionary<Mobile, PlayerDuel> Duels = new Dictionary<Mobile, PlayerDuel>();
        public static void OnLogin(LoginEventArgs e)
        {
            Mobile m = e.Mobile;

            if (Fighters != null)
            {
                if (Fighters.Contains(m))
                    Fighters.Remove(m);

                if (Duels.ContainsKey(m))
                {
                    PlayerDuel duel = Duels[m];
                    var key = Duels.FirstOrDefault(x => x.Value == duel).Key;
                    Duels.Remove(m);
                    Duels.Remove(key);
                }
            }
        }

        public static void EventSink_PlayereDeath(PlayerDeathEventArgs e)
        {
            var killed = e.Mobile;
            var corpse = e.Corpse;


            if (killed != null && Duels.ContainsKey(killed))
            {
                if (killed is PlayerMobile)
                {
                    PlayerGravestone g = new PlayerGravestone(killed as PlayerMobile);
                    g.MoveToWorld(corpse.Location, corpse.Map);
                    g.DropItem(corpse);

                }

                DuelResTimer timer = new DuelResTimer(killed as PlayerMobile);
                timer.Start();
            }
        }

        public static int Notoriety_Handler(Mobile from, IDamageable targ)
        {
            return Notoriety_HandleNotoriety(from, targ);
        }

        private static int Notoriety_HandleNotoriety(Mobile from, IDamageable targ)
        {
            if (from == null || targ == null || !(targ is Mobile))
                return NotorietyHandlers.MobileNotoriety(from, targ);

            Mobile target = targ as Mobile;

            PlayerDuel fromDuel, targetDuel;
            bool fromInDuel = IsInDuel(from, out fromDuel);
            bool targetInDuel = IsInDuel(target, out targetDuel);

            if (fromInDuel && targetInDuel)
            {

                if (fromDuel == null || targetDuel == null)
                    return NotorietyHandlers.MobileNotoriety(from, target);

                if (fromDuel == targetDuel)
                {

                    if (fromDuel.Started)
                    {

                        if (fromDuel.Fighters.Contains(from) && fromDuel.Fighters.Contains(target))
                            return Notoriety.Enemy;
                        //else
                            //return NotorietyHandlers.MobileNotoriety(from, target); // this didn't have return at first?

                    }
                    else
                        return NotorietyHandlers.MobileNotoriety(from, target);

                }
                else
                    return Notoriety.Invulnerable;

            }
            else if ((fromInDuel && !targetInDuel) || (!fromInDuel && targetInDuel))
            {
                if (!target.Player || !from.Player)
                    return NotorietyHandlers.MobileNotoriety(from, target);
                else if (!(target.Region is GuardedRegion))
                    return NotorietyHandlers.MobileNotoriety(from, target);
                else
                    if ((fromInDuel && fromDuel.Started) || (targetInDuel && targetDuel.Started))
                    return Notoriety.Invulnerable;
                else
                    return NotorietyHandlers.MobileNotoriety(from, target);
            }
            else
                return NotorietyHandlers.MobileNotoriety(from, target);

            return NotorietyHandlers.MobileNotoriety(from, target);
        }

        public static bool IsInDuel(Mobile m, out PlayerDuel duel)
        {
            duel = null;

            if (Duels.ContainsKey(m))
            {
                duel = Duels[m];
                return true;
            }

            return false;
        }

        private static bool PlayerMobile_AllowHarmful(Mobile from, IDamageable targ)
        {

            if (from == null || targ == null || !(targ is Mobile))
                return NotorietyHandlers.Mobile_AllowHarmful(from, targ);

            Mobile target = targ as Mobile;

            PlayerDuel fromDuel, targetDuel;
            bool fromInDuel = IsInDuel(from, out fromDuel);
            bool targetInDuel = IsInDuel(target, out targetDuel);

            if (fromInDuel && targetInDuel)
            {
                if (fromDuel == null || targetDuel == null)
                    return NotorietyHandlers.Mobile_AllowHarmful(from, target);

                return (fromDuel == targetDuel);
            }
            else if ((fromInDuel && !targetInDuel) || (targetInDuel && !fromInDuel))
                if (from.Player && target.Player)
                    return false;

            return NotorietyHandlers.Mobile_AllowHarmful(from, target);
        }

        private static bool PlayerMobile_AllowBenificial(Mobile from, Mobile target)
        {
            if (from == null || target == null)
                return NotorietyHandlers.Mobile_AllowBeneficial(from, target); ;

            PlayerDuel fromDuel, targetDuel;
            bool fromInDuel = IsInDuel(from, out fromDuel);
            bool targetInDuel = IsInDuel(target, out targetDuel);

            if (fromInDuel && targetInDuel)
            {
                if (fromDuel == null || targetDuel == null)
                    return NotorietyHandlers.Mobile_AllowBeneficial(from, target);

                return (fromDuel == targetDuel);
            }
            else if ((fromInDuel && !targetInDuel) || (targetInDuel && !fromInDuel))
                if (from.Player && target.Player)
                    return false;

            return NotorietyHandlers.Mobile_AllowHarmful(from, target);
        }

        public static void Initialize()
        {
            CommandSystem.Register("Duel", AccessLevel.Player, new CommandEventHandler(OnCommand_Duel));
            CommandSystem.Register("NoDuel", AccessLevel.Player, new CommandEventHandler(OnCommand_NoDuel));
            CommandSystem.Register("AllowDuel", AccessLevel.Player, new CommandEventHandler(OnCommand_AllowDuel));

            EventSink.PlayerDeath += EventSink_PlayereDeath;
            EventSink.Login += OnLogin;

            PlayerMobile.AllowBeneficialHandler = new AllowBeneficialHandler(PlayerMobile_AllowBenificial);
            PlayerMobile.AllowHarmfulHandler = new AllowHarmfulHandler(PlayerMobile_AllowHarmful);

            Notoriety.Handler += new NotorietyHandler(Notoriety_HandleNotoriety);
        }

        private static void OnCommand_Duel(CommandEventArgs e)
        {
            Mobile m = e.Mobile;

            if (m == null)
                return;

            if (!CanDuel(m))
                return;

            m.Target = new DuelTarget();
        }

        private static void OnCommand_NoDuel(CommandEventArgs e)
        {
            Mobile m = e.Mobile;

            if (m == null)
                return;

            if (OptOut == null)
                OptOut = new List<Mobile>();

            OptOut.Add(m);
            m.SendMessage("You are no longer participating in dueling.");
        }

        private static void OnCommand_AllowDuel(CommandEventArgs e)
        {
            Mobile m = e.Mobile;

            if (m == null)
                return;

            if (OptOut != null && OptOut.Contains(m))
            {
                OptOut.Remove(m);
                m.SendMessage("You are now participating in dueling.");
            }

        }

        public static bool CanDuel(Mobile m)
        {
            if (Fighters.Contains(m) || Blacklist.Contains(m) || OptOut.Contains(m))
                return false;

            return true;
        }

        private class DuelTarget : Target
        {
            public DuelTarget()
                : base(10, false, TargetFlags.None)
            {
            }
            protected override void OnTarget(Mobile from, object o)
            {
                if (o is Mobile && CanDuel(o as Mobile))
                {
                    Mobile defender = o as Mobile;

                    if (defender == null)
                        return;

                    PlayerDuel duel = new PlayerDuel(from, defender);
                    duel.MoveToWorld(defender.Location, defender.Map);
                    Effects.SendLocationEffect(EffectItem.Create(duel.Location, duel.Map, EffectItem.DefaultDuration), duel.Map, 0xABF6, 8, 1, 0, 0); // ground effect
                    defender.SendGump(new ChallengeGump(from, defender, duel));
                    
                }
                else if (o is Mobile && !CanDuel(o as Mobile))
                {
                    from.SendMessage("That player is unable to duel at the moment.");
                }
            }
        }
    }

	public class PlayerDuel: Item
	{
        private int MaxRange = 20;
        private DateTime MessageTime;

        private bool _Started;
        public bool Started
        {
            get { return _Started; }
            set { _Started = value; }

        }

        public List<Mobile> Fighters;

        private PlayerDuelTimer timer;
        public PlayerDuel(Mobile attacker, Mobile defender)
			: base(0x4038)
		{
            Name = "Duel Flag";
            Movable = false;
            MessageTime = DateTime.UtcNow;

            Fighters = new List<Mobile>();
            Timer.DelayCall(TimeSpan.FromSeconds(0.5), () =>
            {
                Fighters.Add(attacker);
                Fighters.Add(defender);
            });

        }

        public PlayerDuel(Serial serial)
            : base(serial) { }

        public override bool HandlesOnMovement { get { return true; } }

        public bool CheckRange(Point3D loc, Point3D oldLoc, int range)
        {
            return this.CheckRange(loc, range) && !this.CheckRange(oldLoc, range);
        }

        public bool CheckRange(Point3D loc, int range)
        {
            return ((this.Z + 8) >= loc.Z && (loc.Z + 16) > this.Z) &&
                   Utility.InRange(this.GetWorldLocation(), loc, range);
        }

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            base.OnMovement(m, oldLocation);

            if (m.Location == oldLocation)
                return;

            if (CheckRange(m.Location, oldLocation, MaxRange) && MessageTime < DateTime.UtcNow)
            {
                MessageTime = DateTime.UtcNow + TimeSpan.FromSeconds(3);
                m.SendMessage("You are leaving the duel range. If you do not return you will forfeit the duel!");
            }
        }
        public void StartDuel(Mobile attacker, Mobile defender)
        {
            //DuelInfo.Fighters.Add(attacker);
            //DuelInfo.Fighters.Add(defender);

            DuelInfo.Duels.Add(attacker, this);
            DuelInfo.Duels.Add(defender, this);

            Started = true;
            attacker.Warmode = true;
            defender.Warmode = true;

            attacker.Combatant = defender;
            defender.Combatant = attacker;

            timer = new PlayerDuelTimer(attacker, defender, this);
            timer.Start();
        }

        public void AttackerWins(Mobile winner, Mobile loser)
        {
            winner.SendMessage("You have won the duel!");
            loser.SendMessage("You have lost the duel!");

            EndDuel(winner, loser);
        }

        public void DefenderWins(Mobile winner, Mobile loser)
        {
            winner.SendMessage("You have won the duel!");
            loser.SendMessage("You have lost the duel!");

            EndDuel(loser, winner);
        }

        public void EndDuel(Mobile attacker, Mobile defender)
        {
            /*
            if (DuelInfo.Fighters != null)
            {
                if (DuelInfo.Fighters.Contains(attacker))
                    DuelInfo.Fighters.Remove(attacker);

                if (DuelInfo.Fighters.Contains(defender))
                    DuelInfo.Fighters.Remove(defender);
            }
            */

            if (DuelInfo.Duels  != null)
            {
                if (DuelInfo.Duels.ContainsKey(attacker))
                    DuelInfo.Duels.Remove(attacker);

                if (DuelInfo.Duels.ContainsKey(defender))
                    DuelInfo.Duels.Remove(defender);
            }

            if (timer  != null)
                timer.Stop();

            Delete();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class PlayerDuelTimer : Timer
    {
        private Mobile Attacker;
        private Mobile Defender;
        private PlayerDuel Flag;
        private DateTime Draw;
        public PlayerDuelTimer(Mobile attacker, Mobile defender, PlayerDuel flag)
            : base(TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(500))
        {
            Attacker = attacker;
            Defender = defender;
            Flag = flag;
            Draw = DateTime.UtcNow + TimeSpan.FromMinutes(1);
        }

        protected override void OnTick()
        {
            if (Flag == null || Attacker == null || Defender == null)
                return;

            if (Draw < DateTime.UtcNow)
            {
                Attacker.SendMessage("The duel ends in a draw.");
                Defender.SendMessage("The duel ends in a draw.");
                Flag.EndDuel(Attacker, Defender);
            }

            if (!Defender.Alive)
                Flag.AttackerWins(Attacker, Defender);
            else if (!Attacker.Alive)
                Flag.DefenderWins(Defender, Attacker);

        }
    }

    public class DuelResTimer : Timer
    {
        PlayerMobile Player;
        public DuelResTimer(PlayerMobile p)
            : base(TimeSpan.FromSeconds(3))
        {
            Player = p;
            p.SendMessage("You have lost the duel and will be resurrected shortly.");
        }

        protected override void OnTick()
        {
            Player.Resurrect();
            Stop();
        }
    }
}


