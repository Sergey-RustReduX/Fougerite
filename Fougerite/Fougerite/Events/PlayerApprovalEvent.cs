﻿
namespace Fougerite.Events
{
    using uLink;

    public class PlayerApprovalEvent
    {
        private readonly ConnectionAcceptor _ca;
        private readonly NetworkPlayerApproval _approval;
        private readonly ClientConnection _cc;
        private readonly bool _deny;
        private bool _ForceAccept = false;

        public PlayerApprovalEvent(ConnectionAcceptor ca, NetworkPlayerApproval approval, ClientConnection cc, bool AboutToDeny)
        {
            this._ca = ca;
            this._cc = cc;
            this._approval = approval;
            this._deny = AboutToDeny;
        }

        public ConnectionAcceptor ConnectionAcceptor
        {
            get { return _ca; }
        }

        public ClientConnection ClientConnection
        {
            get { return _cc; }
        }

        public NetworkPlayerApproval NetworkPlayerApproval
        {
            get { return _approval; }
        }

        public bool AboutToDeny
        {
            get { return _deny; }
        }

        public bool ForceAccept
        {
            get { return _ForceAccept; }
            set { _ForceAccept = value; }
        }

        public bool ServerHasPlayer
        {
            get
            {
                if (Fougerite.Server.Cache.ContainsKey(_cc.UserID))
                {
                    return Fougerite.Server.Cache[_cc.UserID].IsOnline;
                }
                return false;
            }
        }
    }
}
