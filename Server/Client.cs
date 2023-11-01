using System.Collections.Generic;
using UnityEngine;
using System.Text;
using StarterAssets.Packet;

namespace Server
{
    class Client
    {
        internal class StateHistory
        {
            public Vector3 position;
            public Quaternion rotation;
            public StateHistory(Vector3 pos, Quaternion cam)
            {
                this.position = pos;
                this.rotation = cam;
            }
        }

        public int id;
        public Dictionary<int, StateHistory> history;
        public PacketDatagram pd;
        public Vector3 pos;
        public Quaternion cam;
        public int lastSeqNumber;

        public Client(int _id, PacketDatagram _pd)
        {
            id = _id;
            pd = _pd;
            lastSeqNumber = 0;
            history = new Dictionary<int, StateHistory>();
            history.Add(0, new StateHistory(new Vector3(pd.playerPosPacket.x, pd.playerPosPacket.y, pd.playerPosPacket.z), new Quaternion(pd.playerCamPacket.x, pd.playerCamPacket.y, pd.playerCamPacket.z, pd.playerCamPacket.w)));
        }

        public void UpdateStateHistory(int seqNumber)
        {
            history.Add(seqNumber, new Client.StateHistory(pos, cam));
            bool suc = history.Remove(lastSeqNumber - 50);
        }

        public override string ToString()
        {
            /* example: "25 c0t 1 2 3" */
            StringBuilder str = new StringBuilder();
            str.Append(lastSeqNumber);
            str.Append(" ");
            str.Append(id);
            str.Append(" ");
            str.Append(pos.x);
            str.Append(" ");
            str.Append(pos.y);
            str.Append(" ");
            str.Append(pos.z);
            return str.ToString();
        }
    }
}
