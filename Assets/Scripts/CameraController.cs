using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour {

    public Vector3 targetOfs;
    public float delay = 0.1f;
    public float rotateSpeed = 90.0f;
    public float switchDelay = 1.0f;

    private Transform mTarget;

    private Vector3 mLastPosition;
    private Quaternion mLastRotation;
    private bool mSwitching;
    private float mSwitchingCurTime;
    private Vector3 mCurVel;

    public Transform target {
        get { return mTarget; }
        set {
            if(value) {
                if(mTarget) {
                    mLastPosition = transform.position;
                    mLastRotation = transform.rotation;
                    mSwitching = true;
                    mSwitchingCurTime = 0.0f;
                }
            }
            else {
                mSwitching = false;
            }

            mTarget = value;
            mCurVel = Vector3.zero;
        }
    }

    // Update is called once per frame
    void Update() {
        if(mTarget) {
            Vector3 toPosition;
            Quaternion toRotation;

            float dt = Time.deltaTime;

            if(mSwitching) {
                mSwitchingCurTime += dt;

                float t = mSwitchingCurTime/switchDelay;
                if(t > 1.0f) {
                    mSwitching = false;
                    t = 1.0f;
                }

                toPosition = Vector3.Lerp(mLastPosition, mTarget.position, t);
                toRotation = Quaternion.Slerp(mLastRotation, mTarget.rotation, t);
            }
            else {
                toPosition = mTarget.position;
                toRotation = mTarget.rotation;
            }

            if(targetOfs != Vector3.zero)
                toPosition += mTarget.rotation*targetOfs;

            Vector3 pos = transform.position;

            toPosition.z = pos.z;

            transform.position = Vector3.SmoothDamp(pos, toPosition, ref mCurVel, delay, Mathf.Infinity, dt);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation, rotateSpeed * dt);
        }
    }
}
