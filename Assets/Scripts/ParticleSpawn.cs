using UnityEngine;
using System.Collections;

/// <summary>
/// Use this with PoolController
/// </summary>
public class ParticleSpawn : MonoBehaviour {
    public float playDelay = 0.1f;

    private bool mActive = false;

    void OnSpawned() {
        mActive = false;

        if(playDelay > 0)
            Invoke("DoPlay", playDelay);
        else
            DoPlay();
    }

    void OnDespawned() {
        mActive = false;

        CancelInvoke();
        particleSystem.Clear();
    }

    // Update is called once per frame
    void LateUpdate() {
        if(mActive && !particleSystem.IsAlive())
            PoolController.ReleaseAuto(transform);
    }

    void DoPlay() {
        particleSystem.Play();
        mActive = true;
    }
}
