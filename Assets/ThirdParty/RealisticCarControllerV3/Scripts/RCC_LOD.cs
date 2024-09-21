using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// LOD system that enables / disables specific components of the vehicle.
/// </summary>
public class RCC_LOD : MonoBehaviour {

    private float distanceToCamera = 0f;        //  Distance to the RCC Camera.
    public float lodBias = 2f;      //  LOD bias multiplier.

    /// <summary>
    /// LOD group.
    /// </summary>
    [System.Serializable]
    public class LODGroup {

        public List<GameObject> group = new List<GameObject>();     //  Target group that will be enabled / disabled related to distance to the RCC Camera.
        internal RCC_WheelCollider[] wheelColliderGroup;        //  Wheelcollider group.
        public bool active = false;     //  Is this group active right now?

        /// <summary>
        /// Adds gameobject to the group.
        /// </summary>
        /// <param name="add"></param>
        public void Add(GameObject add) {

            //  If group doesn't contain it, add it.
            if (!group.Contains(add))
                group.Add(add);

        }

        /// <summary>
        /// Enables group.
        /// </summary>
        public void EnableGroup() {

            //  If group is active, return.
            if (active)
                return;

            //  Setting all group members to true.
            for (int i = 0; i < group.Count; i++)
                group[i].SetActive(true);

            //  If wheelcollider group is not empty...
            if (wheelColliderGroup != null && wheelColliderGroup.Length > 0) {

                //  Enable aligning wheels and drawing skidmarks.
                for (int i = 0; i < wheelColliderGroup.Length; i++) {

                    wheelColliderGroup[i].alignWheel = true;
                    wheelColliderGroup[i].drawSkid = true;

                }

            }

            //  Group is active right now.
            active = true;

        }

        /// <summary>
        /// Disables group.
        /// </summary>
        public void DisableGroup() {

            //  If group is inactive, return.
            if (!active)
                return;

            //  Setting all group members to false.
            for (int i = 0; i < group.Count; i++)
                group[i].SetActive(false);

            //  If wheelcollider group is not empty...
            if (wheelColliderGroup != null && wheelColliderGroup.Length > 0) {

                //  Disabling aligning wheels and drawing skidmarks.
                for (int i = 0; i < wheelColliderGroup.Length; i++) {

                    wheelColliderGroup[i].alignWheel = false;
                    wheelColliderGroup[i].drawSkid = false;

                }

            }

            //  Group is inactive right now.
            active = false;

        }

    }

    private LODGroup[] lodGroup;        //  All LOD groups.

    private int level = 0;      //  Current level.
    private int oldLevel = -1;      //  Previous level used for detecting level changes.

    private void Awake() {

        //  Creating 4 LOD groups.
        lodGroup = new LODGroup[3];
        lodGroup[0] = new LODGroup();
        lodGroup[1] = new LODGroup();
        lodGroup[2] = new LODGroup();

    }

    private IEnumerator Start() {

        //  All groups will contain audiosources, lights, hood camera, wheel camera, particles, and wheelcolliders.
        //  First group = lights, and wheelcolliders.
        //  Second group = audiosources, hood camera, wheel camera, and particles.

        yield return new WaitForFixedUpdate();

        if (transform.Find("All Audio Sources"))
            lodGroup[1].Add(transform.Find("All Audio Sources").gameObject);

        RCC_Light[] allLights = GetComponentsInChildren<RCC_Light>();

        foreach (RCC_Light item in allLights)
            lodGroup[0].Add(item.gameObject);

        RCC_HoodCamera hoodCamera = GetComponentInChildren<RCC_HoodCamera>();

        if (hoodCamera)
            lodGroup[1].Add(hoodCamera.gameObject);

        RCC_WheelCamera wheelCamera = GetComponentInChildren<RCC_WheelCamera>();

        if (wheelCamera)
            lodGroup[1].Add(wheelCamera.gameObject);

        ParticleSystem[] allParticles = GetComponentsInChildren<ParticleSystem>();

        foreach (ParticleSystem item in allParticles)
            lodGroup[1].Add(item.gameObject);

        lodGroup[0].wheelColliderGroup = GetComponentsInChildren<RCC_WheelCollider>();

    }

    private void Update() {

        //  If RCC Camera found, get the distance.
        if (RCC_SceneManager.Instance.activeMainCamera)
            distanceToCamera = Vector3.Distance(transform.position, RCC_SceneManager.Instance.activeMainCamera.transform.position);

        //  Setting level.
        if (distanceToCamera < 25f * lodBias)
            level = 2;
        else if (distanceToCamera < 50f * lodBias)
            level = 1;
        else if (distanceToCamera < 100f * lodBias)
            level = 0;

        //  If previous level is not same with current level, set new LOD.
        if (level != oldLevel)
            SetLOD();

        //  Setting previous level same with current level.
        oldLevel = level;

    }

    /// <summary>
    /// Sets the LOD.
    /// </summary>
    private void SetLOD() {

        for (int i = level; i >= 0; i--)
            lodGroup[i].EnableGroup();

        int lev = (lodGroup.Length - 1) - level;

        for (int i = 0; i < lev; i++)
            lodGroup[i].DisableGroup();

    }

}
