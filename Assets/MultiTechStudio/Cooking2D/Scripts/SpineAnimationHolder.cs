using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;

namespace MultiTechStudio.Cooking2D
{
    public class SpineAnimationHolder : MonoBehaviour
    {
        public SkeletonGraphic anim;

        public string[] idleAnim;// = "Idle_1";
        public string[] walkAnim;// = "Walk_1";
        public string[] happyAnim;// = "Wow";
        public string[] sadAnim;// = "Angry";

        // Start is called before the first frame update
        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void SetIdleAnim()
        {
            anim.AnimationState.SetAnimation(0, idleAnim[Random.Range(0, idleAnim.Length)], true);
        }
        public void SetWalkAnim(bool _preSet = false, bool _direction = false)
        {
            if (!_preSet)
                anim.AnimationState.SetAnimation(0, walkAnim[Random.Range(0, walkAnim.Length)], true);
            else
            {
                if (_direction)
                {
                    string _anim = walkAnim[Random.Range(0, walkAnim.Length / 2)];
                    // Debug.Log(transform.parent.name + ":" + _anim);
                    anim.AnimationState.SetAnimation(0, _anim, true);
                }
                else
                {
                    string _anim = walkAnim[Random.Range(walkAnim.Length / 2, walkAnim.Length)];
                    // Debug.Log(_anim);
                    anim.AnimationState.SetAnimation(0, _anim, true);
                }
            }
        }
        public void SetHappyAnim()
        {
            anim.AnimationState.SetAnimation(0, happyAnim[Random.Range(0, happyAnim.Length)], true);
        }
        public void SetSadAnim()
        {
            anim.AnimationState.SetAnimation(0, sadAnim[Random.Range(0, sadAnim.Length)], true);
        }
    }
}
