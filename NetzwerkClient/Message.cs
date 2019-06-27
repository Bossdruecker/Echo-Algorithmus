using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetzwerkClientUDP
{
    class Message
    {
        public enum MsgCommand
        {
            EXIT,
            INFO,
            ECHO,
            RESULT,
            RESET
        }

        public Verbindung my_verbindung;
        public Verbindung to_verbindung;
        public MsgCommand command;
        public int sum;

        public Message(Verbindung my_verbindung, Verbindung to_verbindung, MsgCommand command, int sum)
        {
            this.my_verbindung = my_verbindung;
            this.to_verbindung = to_verbindung;
            this.command = command;
            this.sum = sum;
        }
    }
}
