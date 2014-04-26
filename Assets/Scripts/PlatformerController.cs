using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlatformerController : RigidBodyController {
    public delegate void Callback(PlatformerController ctrl);

    [SerializeField]
    Transform _eye;

    public float eyeOrientSpeed = 180.0f; //when we lock the eye again, this is the speed to re-orient based on dirHolder
    public float eyePositionDelay = 0.1f; //reposition delay when we lock the eye again
    public Vector3 eyeOfs;

    public int jumpCounter = 1;
    public float jumpImpulse = 2f;
    public float jumpWallImpulse = 8f;
    public float jumpWallUpImpulse = 4f;
    public float jumpWaterForce = 5f;
    public float jumpForce = 80.0f;
    public float jumpDelay = 0.1f;

    public bool jumpWall = false; //wall jump
    public float jumpWallLockDelay = 0.1f;

    public bool jumpDropAllow = true; //if true, player can jump when they are going down
    public float jumpAirDelay = 0.1f; //allow player to jump if they are off the ground for a short time.

    public bool slideAllowJump = false;

    public float airDampForceX; //force to try to reduce the horizontal speed while mid-air
    public float airDampMinSpeedX; //minimum criteria of horizontal speed when dampening

    public bool wallStick = true;
    public bool wallStickPush = false; //if true, player must press the direction towards the wall to stick
    public bool wallStickDownOnly = false; //if true, only stick to wall if we are going downwards
    public float wallStickAngleOfs = 10.0f; //what angle is acceptible to wall stick, within 90 based on dirHolder's up
    public float wallStickDelay = 0.2f; //delay to stick to wall when moving against one
    public float wallStickUpDelay = 0.2f; //how long to move up the wall once you stick
    public float wallStickUpForce = 60f; //slightly move up the wall
    public float wallStickForce = 40f; //move towards the wall
    public float wallStickDownEaseDelay = 0.0f; //if not zero, ease into speed cap from 0
    public float wallStickDownSpeedCap = 5.0f; //reduce speed upon sticking to wall if going downward, 'friction'
    public LayerMask wallStickInvalidMask; //layer masks that do not allow wall stick

    public LayerMask plankLayer;
    public bool plankEnableDrop; //by holding down arrow, drop down
    public float plankDropDelay; //hold down long enough to drop from plank
    public float plankCheckDelay; //delay to check if we can revert plank collision

    public int player = 0;
    public int moveInputX = InputManager.ActionInvalid;
    public int moveInputY = InputManager.ActionInvalid;
    public int jumpInput = InputManager.ActionInvalid;

    public bool startInputEnabled = false;
    public bool jumpReleaseClearVelocity = false;
    public float jumpReleaseClearVelocityTo = 0.0f;

    public event Callback landCallback;
    public event Callback jumpCallback;

    private const string mInvokePlankEndIgnore = "OnPlankEndIgnore";

    private bool mInputEnabled = false;

    private bool mJump = false;
    private int mJumpCounter = 0;
    private float mJumpLastTime = 0.0f;
    private bool mJumpingWall = false;
    private bool mJumpInputDown = false;

    private bool mEyeLocked = true;
    private Vector3 mEyeOrientVel;
    private bool mLastGround = false;

    private bool mWallSticking = false;
    private float mWallStickLastTime = 0.0f;
    private float mWallStickLastInputTime = 0.0f;
    private CollideInfo mWallStickCollInfo;
    private M8.MathUtil.Side mWallStickSide;
    private bool mWallStickWaitInput;

    private bool mIsOnPlatform;
    private int mIsOnPlatformLayerMask;

    private bool mMoveSideLock;

    private bool[] mPlankIsIgnore;
    private bool mPlankCheckActive;
    private int[] mPlankLayerIndices;

    private float mMoveYGround;
    private float mMoveYGroundDownLastTime; //last time the down key was pressed

    private float mLastGroundTime; //last time we were on ground

    private List<Collider> mTriggers = new List<Collider>(4); //triggers we entered

    public bool inputEnabled {
        get { return mInputEnabled; }
        set {
            if(mInputEnabled != value) {
                mInputEnabled = value;

                InputManager input = Main.instance != null ? Main.instance.input : null;
                if(input != null) {
                    if(mInputEnabled) {
                        if(jumpInput != InputManager.ActionInvalid)
                            input.AddButtonCall(player, jumpInput, OnInputJump);
                    }
                    else {
                        if(jumpInput != InputManager.ActionInvalid)
                            input.RemoveButtonCall(player, jumpInput, OnInputJump);

                        mJumpInputDown = false;

                        mMoveYGround = 0.0f;
                    }
                }
            }
        }
    }

    public Transform eye {
        get { return _eye; }
    }

    /// <summary>
    /// This determines whether or not the eye will be set to the dirHolder's transform.
    /// default: true. If false, input for looking up/down will be disabled.
    /// </summary>
    public bool eyeLocked {
        get { return _eye != null && mEyeLocked; }
        set {
            //if(mEyeLocked != value) {
            mEyeLocked = value;
            //}
        }
    }

    public int jumpCounterCurrent { get { return mJumpCounter; } set { mJumpCounter = value; } }

    /// <summary>
    /// Note: Fixed time.
    /// </summary>
    public float jumpLastTime { get { return mJumpLastTime; } }

    public bool isJump { get { return mJump; } }

    public bool isJumpWall { get { return mJumpingWall; } }

    public bool isWallStick { get { return mWallSticking; } }

    public bool isOnPlatform { get { return mIsOnPlatform; } }

    public float wallStickLastTime { get { return mWallStickLastTime; } }
    public CollideInfo wallStickCollide { get { return mWallStickCollInfo; } }
    public M8.MathUtil.Side wallStickSide { get { return mWallStickSide; } }

    public bool canWallJump {
        get { return jumpWall && mWallSticking; }
    }

    /// <summary>
    /// Set to true for manual use of moveSide
    /// </summary>
    public bool moveSideLock {
        get { return mMoveSideLock; }
        set { mMoveSideLock = value; }
    }

    public List<Collider> triggers { get { return mTriggers; } }

    public override void ResetCollision() {
        base.ResetCollision();

        mLastGround = false;
        mLastGroundTime = 0.0f;
        mJump = false;
        mJumpingWall = false;

        lockDrag = false;

        mWallSticking = false;
        mWallStickWaitInput = false;

        mIsOnPlatform = false;
        mIsOnPlatformLayerMask = 0;

        //clear planking
        if(mPlankLayerIndices != null && mPlankLayerIndices.Length > 0) {
            for(int i = 0; i < mPlankLayerIndices.Length; i++) {
                if(mPlankIsIgnore[i]) {
                    mPlankIsIgnore[i] = false;
                    Physics.IgnoreLayerCollision(gameObject.layer, mPlankLayerIndices[i], false);
                }
            }
        }
    }

    /// <summary>
    /// Call this if you want to update the camera manually (usu. when you disable this controller but still want camera update)
    /// </summary>
    public void UpdateCamera(float deltaTime) {
        if(_eye != null && mEyeLocked) {
            Quaternion dirRot = dirHolder.rotation;

            Vector3 pos;// = dirHolder.position + dirHolder.localToWorldMatrix.MultiplyPoint(eyeOfs);
            if(eyeOfs != Vector3.zero) {
                pos = dirHolder.position + dirRot * eyeOfs;
            }
            else {
                pos = dirHolder.position;
            }

            bool posDone = _eye.position == pos;
            if(!posDone) {
                _eye.position = Vector3.SmoothDamp(_eye.position, pos, ref mEyeOrientVel, eyePositionDelay, Mathf.Infinity, deltaTime);
            }
            else
                mEyeOrientVel = Vector3.zero;

            bool rotDone = _eye.rotation == dirRot;
            if(!rotDone) {
                float step = eyeOrientSpeed * Time.fixedDeltaTime;
                _eye.rotation = Quaternion.RotateTowards(_eye.rotation, dirRot, step);
            }
        }
    }

    public void _PlatformSweep(bool isOn, int layer) {
        if(mIsOnPlatform != isOn) {
            mIsOnPlatform = isOn;
            mIsOnPlatformLayerMask = 1 << layer;

            RefreshCollInfo();
        }

        //if(!isOn && mJump)
        //mJumpLastTime = Time.fixedTime;
    }

    /// <summary>
    /// Call this for manual input jumping
    /// </summary>
    public void Jump(bool down) {
        if(mJumpInputDown != down && !rigidbody.isKinematic) {
            mJumpInputDown = down;

            if(mJumpInputDown) {
                if(isUnderWater) {
                    mJumpingWall = false;
                    mJump = true;
                    mJumpCounter = 0;
                }
                else if(canWallJump) {

                    rigidbody.velocity = Vector3.zero;
                    lockDrag = true;
                    rigidbody.drag = airDrag;

                    Vector3 impulse = mWallStickCollInfo.normal * jumpWallImpulse;
                    impulse += dirHolder.up * jumpWallUpImpulse;

                    PrepJumpVel();
                    rigidbody.AddForce(impulse, ForceMode.Impulse);

                    mJumpingWall = true;
                    mJump = true;

                    mWallSticking = false;

                    mJumpLastTime = Time.fixedTime;
                    //mJumpCounter = Mathf.Clamp(mJumpCounter + 1, 0, jumpCounter);

                    mJumpCounter = 1;

                    if(jumpCallback != null)
                        jumpCallback(this);
                }
                else if(!isSlopSlide || slideAllowJump) {
                    if(isGrounded || isSlopSlide || (mJumpCounter < jumpCounter && (Time.fixedTime - mLastGroundTime < jumpAirDelay || jumpDropAllow || mJumpCounter > 0))) {
                        lockDrag = true;
                        rigidbody.drag = airDrag;

                        PrepJumpVel();

                        rigidbody.AddForce(dirHolder.up * jumpImpulse, ForceMode.Impulse);

                        mJumpCounter++;
                        mJumpingWall = false;

                        mWallSticking = false;

                        mJump = true;
                        mJumpLastTime = Time.fixedTime;

                        if(jumpCallback != null)
                            jumpCallback(this);
                    }
                }
            }
            else {
                if(jumpReleaseClearVelocity) {
                    Vector3 lv = localVelocity;
                    if(lv.y > jumpReleaseClearVelocityTo) {
                        lv.y = jumpReleaseClearVelocityTo;
                        localVelocity = lv;
                    }
                }
            }
        }
    }

    protected override void WaterEnter() {
        mJumpCounter = 0;
        mJumpingWall = false;
    }

    protected override void WaterExit() {
        if(mJump) {
            mJumpLastTime = Time.fixedTime;
        }
    }

    protected override bool CanMove(Vector3 dir, float maxSpeed) {

        //float x = localVelocity.x;
        float d = localVelocity.x * localVelocity.x;

        //disregard y (for better air controller)

        bool ret = d < maxSpeed * maxSpeed;

        //see if we are trying to move the opposite dir
        if(!ret) { //see if we are trying to move the opposite dir
            Vector3 velDir = rigidbody.velocity.normalized;
            ret = Vector3.Dot(dir, velDir) < moveCosCheck;
        }

        return ret;
    }

    float WallStickCurrentDownCap() {
        if(wallStickDownEaseDelay <= 0.0f || Time.fixedTime - mWallStickLastTime >= wallStickDownEaseDelay)
            return wallStickDownSpeedCap;

        return Holoville.HOTween.Core.Easing.Sine.EaseIn(Time.fixedTime - mWallStickLastTime, 0.0f, wallStickDownSpeedCap, wallStickDownEaseDelay, 0, 0);
    }

    protected override void RefreshCollInfo() {
        //plank check, see if we need to ignore it
        if(plankLayer != 0) {
            //check if there's a coll that is a plank
            int plankFoundLayerInd = -1;
            bool plankFound = false;
            CollisionFlags plankCollFlag = CollisionFlags.None;

            for(int i = 0; i < mCollCount; i++) {
                CollideInfo inf = mColls[i];
                if(inf.collider == null || inf.collider.gameObject == null || !inf.collider.gameObject.activeInHierarchy) {
                    RemoveColl(i);
                    i--;
                }
                else if(((1 << inf.collider.gameObject.layer) & plankLayer) != 0) {
                    plankFoundLayerInd = inf.collider.gameObject.layer;
                    plankFound = true;
                    plankCollFlag = inf.flag;
                    if(plankCollFlag != CollisionFlags.Below) {
                        RemoveColl(i);
                        i--;
                    }
                }
            }

            if(plankFound) {
                if(plankCollFlag == CollisionFlags.Below) {
                    //check if we are ready to drop
                    if(plankEnableDrop && mMoveYGround < 0.0f && Time.fixedTime - mMoveYGroundDownLastTime >= plankDropDelay) {
                        SetPlankingIgnore(plankFoundLayerInd, true);
                    }
                }
                else {
                    SetLocalVelocityToBody(); //revert rigidbody's velocity :P
                    SetPlankingIgnore(plankFoundLayerInd, true);
                }
            }
            else if(isGrounded && mMoveYGround < 0.0f) {
                mMoveYGround = 0.0f;
            }
        }

        base.RefreshCollInfo();

        //bool isGroundColl = (mCollFlags & CollisionFlags.Below) != 0;

        if(mIsOnPlatform) {
            mCollFlags |= CollisionFlags.Below;
            mCollGroundLayerMask |= mIsOnPlatformLayerMask;
        }

        bool lastWallStick = mWallSticking;
        mWallSticking = false;

        if(isSlopSlide) {
            //Debug.Log("sliding");
            mLastGround = false;
            mJumpCounter = jumpCounter;
        }
        //refresh wallstick
        else if(wallStick && !mJumpingWall && collisionFlags == CollisionFlags.Sides) {
            //check if we are going up
            if(!wallStickDownOnly || localVelocity.y <= 0.0f) {
                Vector3 up = dirHolder.up;

                if(collisionFlags == CollisionFlags.Sides) {
                    for(int i = 0; i < mCollCount; i++) {
                        CollideInfo inf = mColls[i];
                        if(inf.flag == CollisionFlags.Sides && (wallStickInvalidMask == 0 || ((1<<inf.collider.gameObject.layer) & wallStickInvalidMask) == 0)) {
                            float a = Vector3.Angle(up, inf.normal);
                            if(a >= 90.0f - wallStickAngleOfs && a <= 90.0f + wallStickAngleOfs) {
                                //wallStickForce
                                mWallStickCollInfo = inf;
                                mWallStickSide = M8.MathUtil.CheckSide(mWallStickCollInfo.normal, dirHolder.up);
                                mWallSticking = true;
                                break;
                            }
                        }
                    }
                }
            }

            if(mWallSticking) {
                if(wallStickPush) {
                    if(CheckWallStickIn(moveSide)) {
                        if(!mWallStickWaitInput) {
                            //cancel horizontal movement
                            Vector3 newVel = localVelocity;
                            newVel.x = 0.0f;

                            //reduce downward speed

                            //Debug.Log("la");

                            float yCap = WallStickCurrentDownCap();
                            if(newVel.y < -yCap) newVel.y = -yCap;

                            rigidbody.velocity = dirHolder.rotation * newVel;

                            mWallStickWaitInput = true;
                        }

                        if(!lastWallStick)
                            mWallStickLastTime = Time.fixedTime;

                        mWallStickLastInputTime = Time.fixedTime;
                    }
                    else {
                        bool wallStickExpired = Time.fixedTime - mWallStickLastInputTime > wallStickDelay;

                        if(wallStickExpired) {
                            mWallStickWaitInput = false;
                            mWallSticking = false;
                        }
                    }
                }
                else {
                    bool wallStickExpired = Time.fixedTime - mWallStickLastTime > wallStickDelay;

                    //see if we are moving away
                    if((wallStickExpired && CheckWallStickMoveAway(moveSide))) {
                        if(!mWallStickWaitInput) {
                            mWallSticking = false;
                        }
                    }
                    else if(!lastWallStick) {
                        mWallStickWaitInput = true;
                        mWallStickLastTime = Time.fixedTime;

                        //cancel horizontal movement
                        Vector3 newVel = localVelocity;
                        newVel.x = 0.0f;

                        //reduce downward speed
                        float yCap = WallStickCurrentDownCap();
                        if(newVel.y < -yCap) newVel.y = -yCap;

                        rigidbody.velocity = dirHolder.rotation * newVel;
                    }
                }
            }

            if(mWallSticking != lastWallStick) {
                if(mWallSticking) {
                    mJump = false;
                    lockDrag = false;
                }
                else {
                    if(wallStickPush)
                        mWallStickWaitInput = false;
                }
            }
        }

        if(mLastGround != isGrounded) {
            if(!mLastGround) {
                //Debug.Log("landed");
                //mJump = false;
                //mJumpingWall = false;
                mJumpCounter = 0;

                if(localVelocity.y <= 0.0f) {
                    if(landCallback != null)
                        landCallback(this);
                }
            }
            else {
                //falling down?
                /*if(mJumpCounter <= 0)
                    mJumpCounter = 1;*/

                mJumpLastTime = Time.fixedTime;
                mLastGroundTime = Time.fixedTime;
            }

            mLastGround = isGrounded;
        }
    }

    protected override void OnDestroy() {
        inputEnabled = false;

        landCallback = null;
        jumpCallback = null;

        base.OnDestroy();
    }

    protected override void OnDisable() {
        base.OnDisable();

    }

    protected override void Awake() {
        base.Awake();

        if(plankLayer != 0) {
            mPlankLayerIndices = M8.PhysicsUtil.GetLayerIndices(plankLayer);
            mPlankIsIgnore = new bool[mPlankLayerIndices.Length];
        }
    }

    // Use this for initialization
    protected override void Start() {
        base.Start();

        inputEnabled = startInputEnabled;
    }

    // Update is called once per frame
    protected override void FixedUpdate() {
        Rigidbody body = rigidbody;
        Quaternion dirRot = dirHolder.rotation;

        if(mInputEnabled) {
            InputManager input = Main.instance.input;

            float moveX, moveY;

            moveX = moveInputX != InputManager.ActionInvalid ? input.GetAxis(player, moveInputX) : 0.0f;
            moveY = moveInputY != InputManager.ActionInvalid ? input.GetAxis(player, moveInputY) : 0.0f;

            //movement
            moveForward = 0.0f;

            if(!mMoveSideLock)
                moveSide = 0.0f;

            if(isUnderWater && !isGrounded) {
                //move forward upwards
                Move(dirRot, Vector3.up, Vector3.right, new Vector2(moveX, moveY), moveForce);
            }
            else if(mWallSticking) {
                if(wallStickPush) {
                    if(!mMoveSideLock)
                        moveSide = moveX;
                }
                else {
                    if(mWallStickWaitInput) {
                        if(CheckWallStickMoveAway(moveX)) {
                            mWallStickWaitInput = false;
                            mWallStickLastTime = Time.fixedTime;
                        }
                    }
                    else if(Time.fixedTime - mWallStickLastTime > wallStickDelay) {
                        if(!mMoveSideLock)
                            moveSide = moveX;
                    }
                }
            }
            else if(!(isSlopSlide || mJumpingWall)) {
                //moveForward = moveY;
                if(!mMoveSideLock) {
                    moveSide = moveX;
                }

                if(isGrounded) {
                    //set current move Y and down time while on ground
                    float newY = moveY < -0.1f ? -1.0f : moveY > 0.1f ? 1.0f : 0.0f;
                    if(mMoveYGround != newY) {
                        mMoveYGround = newY;
                        if(mMoveYGround < 0.0f)
                            mMoveYGroundDownLastTime = Time.fixedTime;
                    }
                }
                else
                    mMoveYGround = 0.0f;
            }

            //jump
            if(mJump && !mWallSticking) {
                if(isUnderWater) {
                    body.AddForce(dirRot * Vector3.up * jumpWaterForce);
                }
                else {
                    if(!mJumpInputDown || Time.fixedTime - mJumpLastTime >= jumpDelay || collisionFlags == CollisionFlags.Above) {
                        mJump = false;
                        lockDrag = false;
                    }
                    else if(localVelocity.y < airMaxSpeed) {
                        body.AddForce(dirRot * Vector3.up * jumpForce);
                    }
                }
            }
        }
        else {
            moveForward = 0.0f;

            if(!mMoveSideLock)
                moveSide = 0.0f;

            mJump = false;

            mMoveYGround = 0.0f;
        }

        base.FixedUpdate();

        //stick to wall
        if(mWallSticking) {
            //reduce speed falling down
            float yCap = WallStickCurrentDownCap();
            if(localVelocity.y < -yCap) {
                //ComputeLocalVelocity();
                Vector3 newVel = new Vector3(localVelocity.x, -yCap, localVelocity.z);
                body.velocity = dirHolder.rotation * newVel;
            }
            //boost up
            else if(localVelocity.y >= 0.0f) {
                float curT = Time.fixedTime - mWallStickLastTime;
                if(curT <= wallStickUpDelay && Main.instance.input.IsDown(0, jumpInput)) {
                    Vector3 upDir = dirRot * Vector3.up;
                    upDir = M8.MathUtil.Slide(upDir, mWallStickCollInfo.normal);

                    if(localVelocity.y < airMaxSpeed)
                        body.AddForce(upDir * wallStickUpForce);
                }
            }

            //push towards the wall
            body.AddForce(-mWallStickCollInfo.normal * wallStickForce);
        }
        else if(mCollCount == 0) {
            //check if no collision, then try to dampen horizontal speed
            if(airDampForceX != 0.0f && moveSide == 0.0f) {
                if(localVelocity.x < -airDampMinSpeedX || localVelocity.x > airDampMinSpeedX) {
                    Vector3 dir = localVelocity.x < 0.0f ? Vector3.right : Vector3.left;
                    body.AddForce(dirRot * dir * airDampForceX);
                }
            }
        }

        //see if we are jumping wall and falling, then cancel jumpwall
        if(mJumpingWall && Time.fixedTime - mJumpLastTime >= jumpWallLockDelay)
            mJumpingWall = false;

        //set eye rotation
        UpdateCamera(Time.fixedDeltaTime);

        //if(CheckPenetrate(0.1f, plankLayer))
        //Debug.Log("planking");
    }

    /*IEnumerator DoWallStick() {
        yield return new WaitForSeconds(wallStickDelay);

        //see if we still sticking
        WaitForFixedUpdate wait = new WaitForFixedUpdate();

        PrepJumpVel();
        
        while(mWallSticking) {
            rigidbody.AddForce(gravityController.up * wallStickUpForce, ForceMode.Force);

            yield return wait;
        }
    }*/

    void PrepJumpVel() {
        ComputeLocalVelocity();

        Vector3 newVel = localVelocity;

        if(newVel.y < 0.0f)
            newVel.y = 0.0f; //cancel 'falling down'

        newVel = dirHolder.rotation * newVel;
        rigidbody.velocity = newVel;
    }

    void OnInputJump(InputManager.Info dat) {
        if(!enabled)
            return;

        if(dat.state == InputManager.State.Pressed) {
            Jump(true);
        }
        else {
            Jump(false);
        }
    }

    bool CheckWallStickMoveAway(float criteria) {
        return criteria != 0 && ((criteria < 0.0f && mWallStickSide == M8.MathUtil.Side.Right) || (criteria > 0.0f && mWallStickSide == M8.MathUtil.Side.Left));
    }

    bool CheckWallStickIn(float criteria) {
        return criteria != 0 && ((criteria < 0.0f && mWallStickSide == M8.MathUtil.Side.Left) || (criteria > 0.0f && mWallStickSide == M8.MathUtil.Side.Right));
    }

    //heh...
    void SetPlankingIgnore(int layer, bool ignore) {
        if(mPlankLayerIndices == null || mPlankLayerIndices.Length == 0)
            return;

        int ind = -1;
        for(int i = 0; i < mPlankLayerIndices.Length; i++) {
            if(layer == mPlankLayerIndices[i]) {
                ind = i;
                break;
            }
        }

        if(mPlankIsIgnore[ind] != ignore) {
            mPlankIsIgnore[ind] = ignore;
            Physics.IgnoreLayerCollision(gameObject.layer, mPlankLayerIndices[ind], mPlankIsIgnore[ind]);

            if(mPlankIsIgnore[ind]) {
                if(!mPlankCheckActive)
                    StartCoroutine(DoPlankCheck());
            }
        }
    }

    IEnumerator DoPlankCheck() {
        mPlankCheckActive = true;
        WaitForSeconds wait = new WaitForSeconds(plankCheckDelay);

        while(mPlankCheckActive) {
            yield return wait;

            int ignoreCount = 0;
            for(int i = 0, max = mPlankIsIgnore.Length; i < max; i++) {
                if(mPlankIsIgnore[i]) {
                    if(!CheckPenetrate(0.01f, 1<<mPlankLayerIndices[i])) {
                        mPlankIsIgnore[i] = false;
                        Physics.IgnoreLayerCollision(gameObject.layer, mPlankLayerIndices[i], false);
                    }
                    else
                        ignoreCount++;
                }
            }

            mPlankCheckActive = ignoreCount > 0;
        }
    }
}
