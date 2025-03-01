﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SecretHistories.Abstract;
using SecretHistories.Enums;
using SecretHistories.UI;
using UnityEngine.EventSystems;

namespace Assets.Scripts.Application.Infrastructure.Events
{
    public class TokenPayloadChangedArgs
    {
        public Token Token { get; set; }
        public ITokenPayload Payload { get; set; }
        public PayloadChangeType ChangeType { get; set; }
        public Context Context { get; set; }
        public RetirementVFX VFX { get; set; }
        public TokenLocation AtLocation { get; set; }

        public TokenPayloadChangedArgs(ITokenPayload payload, PayloadChangeType changeType)
        {
            Payload = payload;
            ChangeType = changeType;
            Context = new Context(Context.ActionSource.Unknown);
        }

        public TokenPayloadChangedArgs(ITokenPayload payload, PayloadChangeType changeType,Context context)
        {
            Payload = payload;
            ChangeType = changeType;
            Context = context;
        }

        public TokenPayloadChangedArgs(ITokenPayload payload, PayloadChangeType changeType, TokenLocation atLocation)
        {
            Payload = payload;
            ChangeType = changeType;
            Context = new Context(Context.ActionSource.Unknown);
            AtLocation=atLocation;
        }

    }
}
