﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SecretHistories.UI {
    public abstract class CardEffect : MonoBehaviour {

        protected CanvasGroup tokenCanvasGroup;

        public abstract void StartAnim(Transform card);

        public abstract void OnAnimDone();

        public virtual void OnDisable() {
            OnAnimDone();
        }

    }
}