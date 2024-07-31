using System;
using Server;
using Server.Mobiles;

namespace Server.Items
{
    public class PlayerGravestone : Container
    {
        private PlayerMobile Player;
        private GraveTimer _Timer;

        //public ContainerData ContainerData { get { return new ContainerData(0x3C, new Rectangle2D(44, 65, 142, 94), 0x48); } }

        [Constructable]
        public PlayerGravestone(PlayerMobile player)
            : base(0x653C)
        {
            Player = player;
            Name = string.Format("R.I.P. {0}",  player.Name);
            Movable = false;

            _Timer = new GraveTimer(this);
            _Timer.Start();
        }

        [Constructable]
        public PlayerGravestone()
            : base(0x653C) //(0x0E40)
        {
            Name = "R.I.P.";
            Movable = false;

            _Timer = new GraveTimer(this);
            _Timer.Start();
        }

        public PlayerGravestone(Serial serial)
            : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!(from is PlayerMobile))
                return;

            if (from.IsStaff() || from.InRange(GetWorldLocation(), 2))
            {
                if (Player == null)
                    base.OnDoubleClick(from);

                if (from.IsStaff() || Player != null && from == Player)
                    base.OnDoubleClick(from);
                else
                    from.SendMessage("Only the owner of the gravestone may collect their belongings.");
            }
            else
            {
                from.SendLocalizedMessage(500446); // That is too far away.
            }
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

            _Timer = new GraveTimer(this);
            _Timer.Start();
        }

        private class GraveTimer : Timer
        {
            private Item Grave;
            private DateTime EndTime;

            public GraveTimer(Item grave)
            : base(TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(1.0))
            {
                Grave = grave;
                EndTime = DateTime.UtcNow + TimeSpan.FromMinutes(5);
            }

            protected override void OnTick()
            {
                if (Grave.Deleted)
                    Stop();

                if (EndTime < DateTime.UtcNow)
                {
                    Effects.SendLocationParticles(EffectItem.Create(Grave.Location, Grave.Map, EffectItem.DefaultDuration), 0x3728, 10, 10, 2023);
                    Grave.Delete();
                    Stop();
                }
            }
        }
    }
}
