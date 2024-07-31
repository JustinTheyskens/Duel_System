using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Gumps
{
    public class ChallengeGump : Gump
    {
        private Mobile Attacker;
        private Mobile Defender;
        private PlayerDuel Duel;
        public ChallengeGump(Mobile attacker, Mobile defender, PlayerDuel duel)
            : base(250, 250)
        {
            Attacker = attacker;
            Defender = defender;
            Duel = duel;
            AddGumpLayout();
        }

        public void AddGumpLayout()
        {
            int x = 250;
            int y = 150;

            AddPage(0);
            AddBackground(0, 0, x, y, 0x6DB);

            AddHtml(18, 10, x - 18, 32, FormatText(string.Format("{0} Challenges you to a duel!", Attacker.Name), "#990033"), false, false);
            AddHtml(18, 58, x - 18, 32, FormatText("Would you like to accept?", "#F0F8FF"), false, false);

            AddButton(40, y - 35, 0xFB7, 0xFB9, 1, GumpButtonType.Reply, 0);
            AddHtml(75, y - 30, 120, 16, FormatText("Accept", "#F0F8FF"), false, false);

            AddButton(40 + (x / 2), y - 35, 0xFB4, 0xFB6, 2, GumpButtonType.Reply, 0);
            AddHtml(75 + (x / 2), y - 30, 120, 16, FormatText("Decline", "#F0F8FF"), false, false);


        }
        public string FormatText(string val, string color)
        {
            if (color == null)
                return String.Format("<div align=left>{0}</div>", val);
            else
                return String.Format("<BASEFONT COLOR={1}><dic align=left>{0}</div>", val, color);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (info.ButtonID == 0)
                return;

            if (info.ButtonID == 1)
            {
                Defender.SendMessage("You accept the duel.");
                Duel.StartDuel(Attacker, Defender);
                Notoriety.Handler += new NotorietyHandler(DuelInfo.Notoriety_Handler);
            }

            if (info.ButtonID == 2)
            {
                Attacker.SendMessage(string.Format("{0} declines your challenge.", Defender.Name));
                Duel.EndDuel(Attacker, Defender);
            }
        }
    }
}
