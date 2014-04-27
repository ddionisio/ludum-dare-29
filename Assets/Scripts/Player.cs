using UnityEngine;
using System.Collections;

public class Player : EntityBase {
    private static Player mInstance;

    private bool mInputEnabled;
    private PlatformerController mCtrl;
    private PlatformerAnimatorController mCtrlAnim;
    private PlayerStats mStats;
    private bool mAllowPauseTime;
    private int mPauseCounter = 0;

    public static Player instance { get { return mInstance; } }

    public bool inputEnabled {
        get { return mInputEnabled; }
        set {
            if(mInputEnabled != value) {
                mInputEnabled = value;

                InputManager input = Main.instance != null ? Main.instance.input : null;
                if(input) {
                    if(mInputEnabled) {
                        input.AddButtonCall(0, InputAction.Action, OnInputAction);
                        input.AddButtonCall(0, InputAction.Jump, OnInputJump);
                    }
                    else {
                        input.RemoveButtonCall(0, InputAction.Action, OnInputAction);
                        input.RemoveButtonCall(0, InputAction.Jump, OnInputJump);
                    }
                    mCtrl.inputEnabled = mInputEnabled;
                }
            }
        }
    }

    public bool allowPauseTime {
        get { return mAllowPauseTime; }
        set {
            if(mAllowPauseTime != value) {
                mAllowPauseTime = value;

                if(!mAllowPauseTime && mPauseCounter > 0)
                    Main.instance.sceneManager.Resume();
            }
        }
    }

    public PlatformerController controller { get { return mCtrl; } }
    public PlatformerAnimatorController controllerAnim { get { return mCtrlAnim; } }

    public PlayerStats stats { get { return mStats; } }

    protected override void StateChanged() {
        //switch((EntityState)prevState)

        switch((EntityState)state) {
            case EntityState.Normal:
                inputEnabled = true;
                break;

            case EntityState.Dead:
                inputEnabled = false;
                break;
        }
    }

    protected override void OnDespawned() {
        //reset stuff here

        base.OnDespawned();
    }

    protected override void OnDestroy() {
        mInstance = null;

        //dealloc here
        inputEnabled = false;

        InputManager input = Main.instance != null ? Main.instance.input : null;
        if(input) {
            input.RemoveButtonCall(0, InputAction.MenuCancel, OnInputPause);
        }

        base.OnDestroy();
    }

    public override void SpawnFinish() {
        //start ai, player control, etc
        state = (int)EntityState.Normal;
    }

    protected override void SpawnStart() {
        //initialize some things
    }

    protected override void Awake() {
        mInstance = this;

        base.Awake();

        //initialize variables
        Main.instance.input.AddButtonCall(0, InputAction.MenuCancel, OnInputPause);

        mCtrl = GetComponent<PlatformerController>();
        mCtrl.moveInputX = InputAction.MoveX;
        mCtrl.moveInputY = InputAction.MoveY;

        mCtrlAnim = GetComponent<PlatformerAnimatorController>();
        mCtrlAnim.takeFinishCallback += OnAnimCtrlEnd;

        CameraController.instance.target = transform;

        mStats = GetComponent<PlayerStats>();
    }

    // Use this for initialization
    protected override void Start() {
        base.Start();

        //initialize variables from other sources (for communicating with managers, etc.)
    }

    public void Pause(bool pause) {
        if(pause) {
            mPauseCounter++;
            if(mPauseCounter == 1) {
                if(mAllowPauseTime)
                    Main.instance.sceneManager.Pause();

                inputEnabled = false;

                Main.instance.input.RemoveButtonCall(0, InputAction.MenuCancel, OnInputPause);
            }
        }
        else {
            mPauseCounter--;
            if(mPauseCounter == 0) {
                if(mAllowPauseTime)
                    Main.instance.sceneManager.Resume();

                //state != (int)EntityState.Lock &&
                if( state != (int)EntityState.Invalid) {
                    inputEnabled = true;

                    Main.instance.input.AddButtonCall(0, InputAction.MenuCancel, OnInputPause);
                }
            }
        }
    }

    void OnInputAction(InputManager.Info dat) {

    }

    void OnInputJump(InputManager.Info dat) {
        if(dat.state == InputManager.State.Pressed) {
            mCtrl.Jump(true);
        }
        else if(dat.state == InputManager.State.Released) {
            mCtrl.Jump(false);
        }
    }

    void OnInputPause(InputManager.Info dat) {

    }

    void OnAnimCtrlEnd(PlatformerAnimatorController ctrl, AnimatorData anim, AMTakeData take) {

    }
}
