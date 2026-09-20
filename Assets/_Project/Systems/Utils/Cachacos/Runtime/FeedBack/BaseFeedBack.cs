using System;
using UnityEngine;

namespace FeedBack
{
    [Serializable]
    public abstract class BaseFeedBack : IParameters<BaseFeedBack>
    {

        #region FeedBack Methods
        public virtual BaseFeedBack WithParam(Parameters param) => this;
        public virtual void Intialize() { }
        public abstract void Play(MonoBehaviour caller, bool rigth, Action afterFeedback = null);

        #endregion
    }
}

