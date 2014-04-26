using UnityEngine;
using System.Collections;

public class PlatformerAnimatorController : MonoBehaviour {
    public delegate void Callback(PlatformerAnimatorController ctrl);
    public delegate void CallbackTake(PlatformerAnimatorController ctrl, AnimatorData anim, AMTakeData take);

    public bool leftFlip = true;
    public bool defaultLeft = false;

    public AnimatorData anim;
    public PlatformerController controller;
    public Transform renderTarget;

    public string idleTake = "idle";
    public string moveTake = "move";
    public string wallJumpTake = "walljump";
    public string wallStickTake = "wallstick";

    public string[] upTakes = { "up" }; //based on jump counter
    public string[] downTakes = { "down" }; //based on jump counter

    public event Callback flipCallback;
    public event CallbackTake takeFinishCallback;
        
    private bool mIsLeft;
    private string mOverrideTake;

    private bool mLockFacing;

    public bool isLeft {
        get { return mIsLeft; }
        set {
            if(mIsLeft != value) {
                mIsLeft = value;

                RefreshFacing();

                if(flipCallback != null)
                    flipCallback(this);
            }
        }
    }

    public bool lockFacing { get { return mLockFacing; } set { mLockFacing = value; } }

    public void RefreshFacing() {
        bool flip = mIsLeft ? leftFlip : !leftFlip;

        if(renderTarget) {
            Vector3 s = renderTarget.localScale;
            s.x = Mathf.Abs(s.x) * (flip ? -1.0f : 1.0f);
            renderTarget.localScale = s;
        }
    }

    public void ResetAnimation() {
        mOverrideTake = null;
        mIsLeft = defaultLeft;

        RefreshFacing();
    }

    public void PlayOverrideTake(string take) {
        //assume its loop type is 'once'
        if(anim && anim.TakeExists(take)) {
            mOverrideTake = take;
            anim.Play(mOverrideTake);
        }
    }

    public void StopOverrideTake() {
        if(anim && mOverrideTake != null) {
            anim.Stop();
            mOverrideTake = null;
        }
    }

    void OnDestroy() {
        flipCallback = null;
        takeFinishCallback = null;
    }

    void Awake() {
        if(anim == null)
            anim = GetComponent<AnimatorData>();

        if(anim) {
            anim.takeCompleteCallback += OnTakeFinish;
        }

        ResetAnimation();

        if(controller == null)
            controller = GetComponent<PlatformerController>();
    }

	// Update is called once per frame
	void Update() {
        if(controller == null || !string.IsNullOrEmpty(mOverrideTake)) return;

        bool left = mIsLeft;

        if(controller.isJumpWall) {
            if(anim) anim.Play(wallJumpTake);

            left = controller.localVelocity.x < 0.0f;
        }
        else if(controller.isWallStick) {
            if(anim) anim.Play(wallStickTake);

            left = M8.MathUtil.CheckSide(controller.wallStickCollide.normal, controller.dirHolder.up) == M8.MathUtil.Side.Right;

        }
        else {
            if(anim) {
                if(controller.isGrounded) {
                    if(controller.moveSide != 0.0f) {
                        anim.Play(moveTake);
                    }
                    else {
                        anim.Play(idleTake);
                    }
                }
                else {
                    if(controller.localVelocity.y <= 0.0f) {
                        int ind = controller.jumpCounterCurrent > 0 ? controller.jumpCounterCurrent-1 : 0; if(ind >= downTakes.Length) ind = downTakes.Length - 1;
                        anim.Play(downTakes[ind]);
                    }
                    else {
                        int ind = controller.jumpCounterCurrent > 0 ? controller.jumpCounterCurrent-1 : 0; if(ind >= upTakes.Length) ind = upTakes.Length - 1;
                        anim.Play(upTakes[ind]);
                    }
                }
            }

            if(controller.moveSide != 0.0f) {
                left = controller.moveSide < 0.0f;
            }
        }

        if(!mLockFacing)
            isLeft = left;
	}

    void OnTakeFinish(AnimatorData anim, AMTakeData take) {
        if(take.name == mOverrideTake)
            mOverrideTake = null;

        if(takeFinishCallback != null)
            takeFinishCallback(this, anim, take);
    }
}
