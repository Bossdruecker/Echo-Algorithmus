using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetzwerkClientUDP
{
    class Status
    {
        public int AnzahlNachbarn { get; }
        public bool Informed { get; set; }
        public int Informed_Nachbarn { get; set; }
        public Verbindung Upward_Node { get; set; }
        public int Speicher { get; set; }
        public int CountInformed { get; set; }
        public bool Initiator { get; set; }
        public int GesamtSpeicher { get; set; }

        public Status(int AnzahlNachbarn, int Speicher, Verbindung upward_Node = null, bool informed = false, int countinformed = 0)
        {
            this.AnzahlNachbarn = AnzahlNachbarn;
            this.Speicher = Speicher;
            this.Upward_Node = upward_Node;
            this.Informed = informed;
            this.Initiator = false;
            this.CountInformed = countinformed;
            this.GesamtSpeicher = this.Speicher;
            this.Informed_Nachbarn = 0;
        }

        public void resetStatus()
        {
            this.Upward_Node = null;
            this.Informed = false;
            this.Initiator = false;
            this.CountInformed = 0;
            this.GesamtSpeicher = this.Speicher;
            this.Informed_Nachbarn = 0;
        }
    }
}
