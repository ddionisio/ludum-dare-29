using UnityEngine;
using System.Collections;

public class GravityControllerApplyUpTarget : MonoBehaviour {
    public Transform target;

    private GravityController mCtrl;

    void Awake() {
        mCtrl = GetComponent<GravityController>();
    }

	// Update is called once per frame
	void Update () {
        if(target) {
            mCtrl.up = target.up;
        }
	}
}
