using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlatformController : MonoBehaviour {
    public string[] tags;
    public LayerMask layerMask;
    public bool useTrigger;
    public bool jumpBoost;

    //public float velocityAngleDiff = 89.0f;
    //public float normalAngleDiff = 90.0f;

    public enum Dir {
        Up,
        Down,
        Left,
        Right
    }

    public Dir dir = Dir.Up;
    public float ofs = 0.15f;

    public bool upDirLimitEnabled = false; //check collider's up dir angle from this platform's dir
    public float upDirLimit = 15.0f;

    Vector3 mDir;

    List<Collider> mColls = new List<Collider>(8);
    List<PlatformerController> mPlatformers = new List<PlatformerController>(8);
    List<PlatformerController> mPlatformerSweep = new List<PlatformerController>(8);

    bool CheckTags(GameObject go) {
        foreach(string tag in tags) {
            if(go.tag == tag)
                return true;
        }

        return false;
    }

    void SetDir() {
        switch(dir) {
            case Dir.Up:
                mDir = Vector3.up;
                break;
            case Dir.Down:
                mDir = -Vector3.up;
                break;
            case Dir.Left:
                mDir = -Vector3.right;
                break;
            case Dir.Right:
                mDir = Vector3.right;
                break;
        }
    }

    void OnTriggerEnter(Collider col) {
        //Debug.Log("fack");
        if(useTrigger) {
            mColls.Add(col);
        }
    }

    void OnTriggerExit(Collider col) {
        if(useTrigger) {
            mColls.Remove(col);

            //look for platformers no longer in the list
            int removeInd = -1;
            for(int i = 0, max = mPlatformers.Count; i < max; i++) {
                if(mPlatformers[i].collider == col) {
                    mPlatformers[i]._PlatformSweep(false, gameObject.layer);
                    removeInd = i;
                    break;
                }
            }

            if(removeInd != -1)
                mPlatformers.RemoveAt(removeInd);
        }
    }

    void OnDisable() {
        foreach(PlatformerController platformer in mPlatformers) {
            if(platformer) {
                platformer.ResetCollision();
                platformer._PlatformSweep(false, 0);
            }
        }

        mColls.Clear();
        mPlatformers.Clear();
        mPlatformerSweep.Clear();
    }

    void Awake() {
        SetDir();
    }

    // Use this for initialization
    void Start() {

    }

    void ApplyPhysics(Vector3 wDir, Collider col, Vector3 vel) {
        GameObject go = col.gameObject;
        Rigidbody body = go.rigidbody;
        //Vector3 up = go.transform.up;
        
        if(((1 << go.layer) & layerMask) != 0 && CheckTags(go) && (!upDirLimitEnabled || Vector3.Angle(wDir, col.transform.up) <= upDirLimit)) {// && Vector3.Angle(up, hit.normal) >= normalAngleDiff) {
            //Vector3 vel = rigidbody.GetPointVelocity(hit.point);
            
            PlatformerController ctrl = go.GetComponent<PlatformerController>();
            
            bool jumping = ctrl != null && (ctrl.isJump || ctrl.isJumpWall);
            
            Vector3 localV = go.transform.worldToLocalMatrix.MultiplyVector(vel);
            Vector3 nLocalV = jumping && jumpBoost ? new Vector3(0, localV.y > 0 ? localV.y : 0) : new Vector3(localV.x, localV.y < 0 ? localV.y : 0);
            Vector3 nWorldV = go.transform.localToWorldMatrix.MultiplyVector(nLocalV);
            
            if(jumping) {
                if(jumpBoost) {
                    if(!mPlatformers.Contains(ctrl))
                        body.velocity += nWorldV;
                }
                else if(localV.y > 0.0f) {
                    body.MovePosition(body.position + nWorldV * Time.fixedDeltaTime);
                }
            }
            else {
                body.MovePosition(body.position + nWorldV * Time.fixedDeltaTime);
            }
            
            //body.velocity += go.transform.localToWorldMatrix.MultiplyVector(nLocalV);
            
            /*if(velocityAngleDiff == 0 || body.velocity == Vector3.zero || Vector3.Angle(wDir, vel) >= velocityAngleDiff) {
                        body.MovePosition(go.transform.position + vel * Time.fixedDeltaTime);
                        //body.velocity += vel;
                    }*/
            
            if(ctrl && !jumping) {
                mPlatformerSweep.Add(ctrl);
                ctrl._PlatformSweep(true, gameObject.layer);
            }
        }
    }

    // Update is called once per frame
    void FixedUpdate() {
#if UNITY_EDITOR
        SetDir();
#endif
        Vector3 vel = rigidbody.velocity;// GetPointVelocity(hit.point);

        if(vel != Vector3.zero || rigidbody.angularVelocity != Vector3.zero) {
            Vector3 wDir = transform.rotation * mDir;

            if(!useTrigger) {
                mColls.Clear();
                RaycastHit[] hits = rigidbody.SweepTestAll(wDir, ofs);
                for(int i = 0, max = hits.Length; i < max; i++) {
                    mColls.Add(hits[i].collider);
                }
            }

            for(int i = 0, max = mColls.Count; i < max; i++) {
                ApplyPhysics(wDir, mColls[i], vel);
            }

            //look for platformers no longer in the list
            foreach(PlatformerController ctrl in mPlatformers) {
                int ind = mPlatformerSweep.IndexOf(ctrl);
                if(ind == -1)
                    ctrl._PlatformSweep(false, gameObject.layer);
            }

            List<PlatformerController> l = mPlatformers;
            mPlatformers = mPlatformerSweep;
            mPlatformerSweep = l;
            mPlatformerSweep.Clear();
        }
    }
}
