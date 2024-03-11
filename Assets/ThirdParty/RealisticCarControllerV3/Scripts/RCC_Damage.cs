//----------------------------------------------
//            Realistic Car Controller
//
// Copyright © 2014 - 2023 BoneCracker Games
// https://www.bonecrackergames.com
// Buğra Özdoğanlar
//
//----------------------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Damage class.
/// </summary>
[System.Serializable]
public class RCC_Damage {

    internal RCC_CarControllerV3 carController;     //  Car controller.
    public bool automaticInstallation = true;       //  If set to enabled, all parts of the vehicle will be processed. If disabled, each part can be selected individually.

    // Mesh deformation
    [Space()]
    [Header("Mesh Deformation")]
    public bool meshDeformation = true;
    public DeformationMode deformationMode = DeformationMode.Fast;

    public enum DeformationMode { Accurate, Fast }
    [Range(1, 100)] public int damageResolution = 100;      //  Resolution of the deformation.
    public LayerMask damageFilter = -1;     // LayerMask filter. Damage will be taken from the objects with these layers.
    public float damageRadius = .5f;        // Verticies in this radius will be effected on collisions.
    public float damageMultiplier = 1f;     // Damage multiplier.
    public float maximumDamage = .5f;       // Maximum Vert Distance For Limiting Damage. 0 Value Will Disable The Limit.
    private readonly float minimumCollisionImpulse = .5f;       // Minimum collision force.
    private readonly float minimumVertDistanceForDamagedMesh = .002f;        // Comparing Original Vertex Positions Between Last Vertex Positions To Decide Mesh Is Repaired Or Not.

    public struct OriginalMeshVerts { public Vector3[] meshVerts; }     // Struct for Original Mesh Verticies positions.
    public struct OriginalWheelPos { public Vector3 wheelPosition; public Quaternion wheelRotation; }
    public struct MeshCol { public Collider col; public bool created; }

    public OriginalMeshVerts[] originalMeshData;        // Array for struct above.
    public OriginalMeshVerts[] damagedMeshData;     // Array for struct above.
    public OriginalWheelPos[] originalWheelData;       // Array for struct above.
    public OriginalWheelPos[] damagedWheelData;        // Array for struct above.

    [Space()]
    [HideInInspector] public bool repairNow = false;      // Repairing now.
    [HideInInspector] public bool repaired = true;        // Returns true if vehicle is completely repaired.
    private bool deformingNow = false;      //  Deforming the mesh now.
    private bool deformed = true;        //  Returns true if vehicle is completely deformed.
    private float deformationTime = 0f;     //  Timer for deforming the vehicle. 

    [Space()]
    public bool recalculateNormals = true;      //  Recalculate normals while deforming / restoring the mesh.
    public bool recalculateBounds = true;       //  Recalculate bounds while deforming / restoring the mesh.

    // Wheel deformation
    [Space()]
    [Header("Wheel Deformation")]
    public bool wheelDamage = true;     //	Use wheel damage.
    public float wheelDamageRadius = .5f;        //   Wheel damage radius.
    public float wheelDamageMultiplier = 1f;        //  Wheel damage multiplier.
    public bool wheelDetachment = true;     //	Use wheel detachment.

    // Light deformation
    [Space()]
    [Header("Light Deformation")]
    public bool lightDamage = true;     //	Use light damage.
    public float lightDamageRadius = .5f;        //Light damage radius.
    public float lightDamageMultiplier = 1f;        //Light damage multiplier.

    // Part deformation
    [Space()]
    [Header("Part Deformation")]
    public bool partDamage = true;     //	Use part damage.
    public float partDamageRadius = .5f;        //Light damage radius.
    public float partDamageMultiplier = 1f;        //Light damage multiplier.

    [Space()]
    public MeshFilter[] meshFilters;    //  Collected mesh filters.
    public RCC_DetachablePart[] detachableParts;        //  Collected detachable parts.
    public RCC_Light[] lights;      //  Collected lights.
    public RCC_WheelCollider[] wheels;      //  Collected wheels.

    private Vector3 contactPoint = Vector3.zero;
    private Vector3[] contactPoints;

    /// <summary>
    /// Collecting all meshes and detachable parts of the vehicle.
    /// </summary>
    public void Initialize(RCC_CarControllerV3 _carController) {

        //  Getting the main car controller.
        carController = _carController;

        if (automaticInstallation) {

            if (meshDeformation) {

                MeshFilter[] allMeshFilters = carController.gameObject.GetComponentsInChildren<MeshFilter>(true);
                List<MeshFilter> properMeshFilters = new List<MeshFilter>();

                // Model import must be readable. If it's not readable, inform the developer. We don't wanna deform wheel meshes. Exclude any meshes belongs to the wheels.
                foreach (MeshFilter mf in allMeshFilters) {

                    if (mf.mesh != null) {

                        if (!mf.mesh.isReadable)
                            Debug.LogError("Not deformable mesh detected. Mesh of the " + mf.transform.name + " isReadable is false; Read/Write must be enabled in import settings for this model!");
                        else if (!mf.transform.IsChildOf(carController.FrontLeftWheelTransform) && !mf.transform.IsChildOf(carController.FrontRightWheelTransform) && !mf.transform.IsChildOf(carController.RearLeftWheelTransform) && !mf.transform.IsChildOf(carController.RearRightWheelTransform))
                            properMeshFilters.Add(mf);

                    }

                }

                GetMeshes(properMeshFilters.ToArray());

            }

            if (lightDamage)
                GetLights(carController.GetComponentsInChildren<RCC_Light>());

            if (partDamage)
                GetParts(carController.GetComponentsInChildren<RCC_DetachablePart>());

            if (wheelDamage)
                GetWheels(carController.GetComponentsInChildren<RCC_WheelCollider>());

        }

    }

    /// <summary>
    /// Gets all meshes.
    /// </summary>
    /// <param name="allMeshFilters"></param>
    public void GetMeshes(MeshFilter[] allMeshFilters) {

        meshFilters = allMeshFilters;

    }

    /// <summary>
    /// Gets all lights.
    /// </summary>
    /// <param name="allLights"></param>
    public void GetLights(RCC_Light[] allLights) {

        lights = allLights;

    }

    /// <summary>
    /// Gets all detachable parts.
    /// </summary>
    /// <param name="allParts"></param>
    public void GetParts(RCC_DetachablePart[] allParts) {

        detachableParts = allParts;

    }

    /// <summary>
    /// Gets all wheels
    /// </summary>
    /// <param name="allWheels"></param>
    public void GetWheels(RCC_WheelCollider[] allWheels) {

        wheels = allWheels;

    }

    /// <summary>
    /// We will be using two structs for deformed sections. Original part struction, and deformed part struction. 
    /// All damaged meshes and wheel transforms will be using these structs. At this section, we're creating them with original struction.
    /// </summary>
    private void CheckMeshData() {

        originalMeshData = new OriginalMeshVerts[meshFilters.Length];

        for (int i = 0; i < meshFilters.Length; i++)
            originalMeshData[i].meshVerts = meshFilters[i].mesh.vertices;

        damagedMeshData = new OriginalMeshVerts[meshFilters.Length];

        for (int i = 0; i < meshFilters.Length; i++)
            damagedMeshData[i].meshVerts = meshFilters[i].mesh.vertices;

    }

    /// <summary>
    /// We will be using two structs for deformed sections. Original part struction, and deformed part struction. 
    /// All damaged meshes and wheel transforms will be using these structs. At this section, we're creating them with original struction.
    /// </summary>
    private void CheckWheelData() {

        originalWheelData = new OriginalWheelPos[wheels.Length];

        for (int i = 0; i < wheels.Length; i++) {

            originalWheelData[i].wheelPosition = wheels[i].transform.localPosition;
            originalWheelData[i].wheelRotation = wheels[i].transform.localRotation;

        }

        damagedWheelData = new OriginalWheelPos[wheels.Length];

        for (int i = 0; i < wheels.Length; i++) {

            damagedWheelData[i].wheelPosition = wheels[i].transform.localPosition;
            damagedWheelData[i].wheelRotation = wheels[i].transform.localRotation;

        }

    }

    /// <summary>
    /// Moving deformed vertices to their original positions while repairing.
    /// </summary>
    public void UpdateRepair() {

        if (!carController)
            return;

        //  If vehicle is not repaired completely, and repairNow is enabled, restore all deformed meshes to their original structions.
        if (!repaired && repairNow) {

            if (originalMeshData == null || originalMeshData.Length < 1)
                CheckMeshData();

            int k;
            repaired = true;

            //  If deformable mesh is still exists, get all verticies of the mesh first. And then move all single verticies to the original positions. If verticies are close enough to the original
            //  position, repaired = true;
            for (k = 0; k < meshFilters.Length; k++) {

                if (meshFilters[k] != null && meshFilters[k].mesh != null) {

                    //  Get all verticies of the mesh first.
                    Vector3[] vertices = meshFilters[k].mesh.vertices;

                    for (int i = 0; i < vertices.Length; i++) {

                        //  And then move all single verticies to the original positions
                        if (deformationMode == DeformationMode.Accurate)
                            vertices[i] += (originalMeshData[k].meshVerts[i] - vertices[i]) * (Time.deltaTime * 5f);
                        else
                            vertices[i] += (originalMeshData[k].meshVerts[i] - vertices[i]);

                        //  If verticies are close enough to their original positions, repaired = true;
                        if ((originalMeshData[k].meshVerts[i] - vertices[i]).magnitude >= minimumVertDistanceForDamagedMesh)
                            repaired = false;

                    }

                    //  We were using the variable named "vertices" above, therefore we need to set the new verticies to the damaged mesh data.
                    //  Damaged mesh data also restored while repairing with this proccess.
                    damagedMeshData[k].meshVerts = vertices;

                    //  Setting new verticies to the all meshes. Recalculating normals and bounds, and then optimizing. This proccess can be heavy for high poly meshes.
                    //  You may want to disable last three lines.
                    meshFilters[k].mesh.SetVertices(vertices);

                    if (recalculateNormals)
                        meshFilters[k].mesh.RecalculateNormals();

                    if (recalculateBounds)
                        meshFilters[k].mesh.RecalculateBounds();

                }

            }

            for (k = 0; k < wheels.Length; k++) {

                if (wheels[k] != null) {

                    //  Get all verticies of the mesh first.
                    Vector3 wheelPos = wheels[k].transform.localPosition;

                    //  And then move all single verticies to the original positions
                    if (deformationMode == DeformationMode.Accurate)
                        wheelPos += (originalWheelData[k].wheelPosition - wheelPos) * (Time.deltaTime * 5f);
                    else
                        wheelPos += (originalWheelData[k].wheelPosition - wheelPos);

                    //  If verticies are close enough to their original positions, repaired = true;
                    if ((originalWheelData[k].wheelPosition - wheelPos).magnitude >= minimumVertDistanceForDamagedMesh)
                        repaired = false;

                    //  We were using the variable named "vertices" above, therefore we need to set the new verticies to the damaged mesh data.
                    //  Damaged mesh data also restored while repairing with this proccess.
                    damagedWheelData[k].wheelPosition = wheelPos;

                    wheels[k].transform.localPosition = wheelPos;
                    wheels[k].transform.localRotation = Quaternion.identity;

                    if (!wheels[k].gameObject.activeSelf)
                        wheels[k].gameObject.SetActive(true);

                    carController.ESPBroken = false;

                    wheels[k].Inflate();

                }

            }

            //  Repairing and restoring all detachable parts of the vehicle.
            for (int i = 0; i < detachableParts.Length; i++) {

                if (detachableParts[i] != null)
                    detachableParts[i].OnRepair();

            }

            //  Repairing and restoring all lights of the vehicle.
            for (int i = 0; i < lights.Length; i++) {

                if (lights[i] != null)
                    lights[i].OnRepair();

            }

            //  If all meshes are completely restored, make sure repairing now is false.
            if (repaired)
                repairNow = false;

        }

    }

    /// <summary>
    /// Moving vertices of the collided meshes to the damaged positions while deforming.
    /// </summary>
    public void UpdateDamage() {

        if (!carController)
            return;

        if (originalMeshData == null || originalMeshData.Length < 1)
            CheckMeshData();

        //  If vehicle is not deformed completely, and deforming is enabled, deform all meshes to their damaged structions.
        if (!deformed && deformingNow) {

            int k;
            deformed = true;
            deformationTime += Time.deltaTime;

            //  If deformable mesh is still exists, get all verticies of the mesh first. And then move all single verticies to the damaged positions. If verticies are close enough to the original
            //  position, deformed = true;
            for (k = 0; k < meshFilters.Length; k++) {

                if (meshFilters[k] != null && meshFilters[k].mesh != null) {

                    //  Get all verticies of the mesh first.
                    Vector3[] vertices = meshFilters[k].mesh.vertices;

                    //  And then move all single verticies to the damaged positions.
                    for (int i = 0; i < vertices.Length; i++) {

                        if (deformationMode == DeformationMode.Accurate)
                            vertices[i] += (damagedMeshData[k].meshVerts[i] - vertices[i]) * (Time.deltaTime * 5f);
                        else
                            vertices[i] += (damagedMeshData[k].meshVerts[i] - vertices[i]);

                    }

                    //  Setting new verticies to the all meshes. Recalculating normals and bounds, and then optimizing. This proccess can be heavy for high poly meshes.
                    meshFilters[k].mesh.SetVertices(vertices);

                    if (recalculateNormals)
                        meshFilters[k].mesh.RecalculateNormals();

                    if (recalculateBounds)
                        meshFilters[k].mesh.RecalculateBounds();

                }

            }

            for (k = 0; k < wheels.Length; k++) {

                if (wheels[k] != null) {

                    Vector3 vertices = wheels[k].transform.localPosition;

                    if (deformationMode == DeformationMode.Accurate)
                        vertices += (damagedWheelData[k].wheelPosition - vertices) * (Time.deltaTime * 5f);
                    else
                        vertices += (damagedWheelData[k].wheelPosition - vertices);

                    wheels[k].transform.localPosition = vertices;
                    //wheels[k].transform.localRotation = Quaternion.Euler(vertices);

                }

            }

            //  Make sure deforming proccess takes only 1 second.
            if (deformationMode == DeformationMode.Accurate && deformationTime <= 1f)
                deformed = false;

            //  If all meshes are completely deformed, make sure deforming is false and timer is set to 0.
            if (deformed) {

                deformingNow = false;
                deformationTime = 0f;

            }

        }

    }

    /// <summary>
    /// Deforming meshes.
    /// </summary>
    /// <param name="collision"></param>
    /// <param name="impulse"></param>
    private void DamageMesh(float impulse) {

        if (!carController)
            return;

        if (originalMeshData == null || originalMeshData.Length < 1)
            CheckMeshData();

        //  We will be checking all mesh filters with these contact points. If contact point is close enough to the mesh, deformation will be applied.
        for (int i = 0; i < meshFilters.Length; i++) {

            //  If mesh filter is not null, enabled, and has a valid mesh data...
            if (meshFilters[i] != null && meshFilters[i].mesh != null && meshFilters[i].gameObject.activeSelf) {

                //  Getting closest point to the mesh. Distance value will be set to closest point of the mesh - contact point.
                float distance = Vector3.Distance(NearestVertex(meshFilters[i].transform, meshFilters[i], contactPoint), contactPoint);

                //  If distance between contact point and closest point of the mesh is in range...
                if (distance <= damageRadius) {

                    //  Collision direction.
                    Vector3 collisionDirection = contactPoint - carController.transform.position;
                    collisionDirection = -collisionDirection.normalized;

                    //  All vertices of the mesh.
                    Vector3[] vertices = damagedMeshData[i].meshVerts;

                    for (int k = 0; k < vertices.Length; k++) {

                        //  Contact point is a world space unit. We need to transform to the local space unit with mesh origin. Verticies are local space units.
                        Vector3 point = meshFilters[i].transform.InverseTransformPoint(contactPoint);
                        //  Distance between vertex and contact point.
                        float distanceToVert = (point - vertices[k]).magnitude;

                        //  If distance between vertex and contact point is in range...
                        if (distanceToVert <= damageRadius) {

                            //  Default impulse of the collision.
                            float damage = impulse;

                            // The damage should decrease with distance from the contact point.
                            damage -= damage * Mathf.Clamp01(distanceToVert / damageRadius);

                            Quaternion rot = Quaternion.identity;

                            Vector3 vW = carController.transform.TransformPoint(vertices[k]);

                            vW += rot * (collisionDirection * damage * (damageMultiplier / 10f));

                            vertices[k] = carController.transform.InverseTransformPoint(vW);

                            //  If distance between original vertex position and deformed vertex position exceeds limits, make sure they are in the limits.
                            if (maximumDamage > 0 && ((vertices[k] - originalMeshData[i].meshVerts[k]).magnitude) > maximumDamage)
                                vertices[k] = originalMeshData[i].meshVerts[k] + (vertices[k] - originalMeshData[i].meshVerts[k]).normalized * (maximumDamage);

                        }

                    }

                }

            }

        }

    }

    /// <summary>
    /// Deforming wheels. Actually changing their local positions and rotations based on the impact.
    /// </summary>
    /// <param name="collision"></param>
    /// <param name="impulse"></param>
    private void DamageWheel(float impulse) {

        if (!carController)
            return;

        if (originalWheelData == null || originalWheelData.Length < 1)
            CheckWheelData();

        for (int i = 0; i < wheels.Length; i++) {

            if (wheels[i] != null && wheels[i].gameObject.activeSelf) {

                Vector3 wheelPos = damagedWheelData[i].wheelPosition;

                Vector3 collisionDirection = contactPoint - carController.transform.position;
                collisionDirection = -collisionDirection.normalized;

                Vector3 closestPoint = wheels[i].WheelCollider.ClosestPointOnBounds(contactPoint);
                float distance = Vector3.Distance(closestPoint, contactPoint);

                if (distance < wheelDamageRadius) {

                    float damage = (impulse * wheelDamageMultiplier) / 30f;

                    // The damage should decrease with distance from the contact point.
                    damage -= damage * Mathf.Clamp01(distance / wheelDamageRadius);

                    Vector3 vW = carController.transform.TransformPoint(wheelPos);

                    vW += (collisionDirection * damage);

                    wheelPos = carController.transform.InverseTransformPoint(vW);

                    if (maximumDamage > 0 && ((wheelPos - originalWheelData[i].wheelPosition).magnitude) > maximumDamage) {

                        //wheelPos = originalWheelData[i].wheelPosition + (wheelPos - originalWheelData[i].wheelPosition).normalized * (maximumDamage);

                        if (wheelDetachment && wheels[i].gameObject.activeSelf)
                            DetachWheel(wheels[i]);

                    }

                    damagedWheelData[i].wheelPosition = wheelPos;

                }

            }

        }

    }
    /// <summary>
    /// Deforming the detachable parts.
    /// </summary>
    /// <param name="collision"></param>
    /// <param name="impulse"></param>
    private void DamagePart(float impulse) {

        if (!carController)
            return;

        if (detachableParts != null && detachableParts.Length >= 1) {

            for (int i = 0; i < detachableParts.Length; i++) {

                if (detachableParts[i] != null && detachableParts[i].gameObject.activeSelf) {

                    if (detachableParts[i].partCollider != null) {

                        Vector3 closestPoint = detachableParts[i].partCollider.ClosestPointOnBounds(contactPoint);
                        float distance = Vector3.Distance(closestPoint, contactPoint);
                        float damage = impulse * partDamageMultiplier;

                        // The damage should decrease with distance from the contact point.
                        damage -= damage * Mathf.Clamp01(distance / damageRadius);

                        if (distance <= damageRadius)
                            detachableParts[i].OnCollision(damage);

                    } else {

                        if ((contactPoint - detachableParts[i].transform.position).magnitude < 1f)
                            detachableParts[i].OnCollision(impulse);

                    }

                }

            }

        }

    }

    /// <summary>
    /// Deforming the lights.
    /// </summary>
    /// <param name="collision"></param>
    /// <param name="impulse"></param>
    private void DamageLight(float impulse) {

        if (!carController)
            return;

        if (lights != null && lights.Length >= 1) {

            for (int i = 0; i < lights.Length; i++) {

                if (lights[i] != null && lights[i].gameObject.activeSelf) {

                    if ((contactPoint - lights[i].transform.position).magnitude < lightDamageRadius)
                        lights[i].OnCollision(impulse * lightDamageMultiplier);

                }

            }

        }

    }

    /// <summary>
    /// Detaches the target wheel.
    /// </summary>
    /// <param name="wheelCollider"></param>
    public void DetachWheel(RCC_WheelCollider wheelCollider) {

        if (!carController)
            return;

        if (!wheelCollider)
            return;

        if (!wheelCollider.gameObject.activeSelf)
            return;

        wheelCollider.gameObject.SetActive(false);
        Transform wheelModel = wheelCollider.wheelModel;

        GameObject clonedWheel = GameObject.Instantiate(wheelModel.gameObject, wheelModel.transform.position, wheelModel.transform.rotation, null);
        clonedWheel.SetActive(true);
        clonedWheel.AddComponent<Rigidbody>();

        GameObject clonedMeshCollider = new GameObject("Mesh Collider");
        clonedMeshCollider.transform.SetParent(clonedWheel.transform, false);
        clonedMeshCollider.transform.position = RCC_GetBounds.GetBoundsCenter(clonedWheel.transform);
        MeshCollider mc = clonedMeshCollider.AddComponent<MeshCollider>();
        MeshFilter biggestMesh = RCC_GetBounds.GetBiggestMesh(clonedWheel.transform);
        mc.sharedMesh = biggestMesh.mesh;
        mc.convex = true;

        carController.ESPBroken = true;

    }

    /// <summary>
    /// Raises the collision enter event.
    /// </summary>
    /// <param name="collision">Collision.</param>
    public void OnCollision(Collision collision) {

        if (!carController)
            return;

        if (!carController.useDamage)
            return;

        if (((1 << collision.gameObject.layer) & damageFilter) != 0) {

            float impulse = collision.impulse.magnitude / 10000f;

            if (collision.rigidbody)
                impulse *= collision.rigidbody.mass / 1000f;
            
            if (impulse < minimumCollisionImpulse)
                impulse = 0f;

            if (impulse > 10f)
                impulse = 10f;

            if (impulse > 0f) {

                deformingNow = true;
                deformed = false;

                repairNow = false;
                repaired = false;

                //  First, we are getting all contact points.
                ContactPoint[] contacts = collision.contacts;
                contactPoints = new Vector3[contacts.Length];

                for (int i = 0; i < contactPoints.Length; i++)
                    contactPoints[i] = contacts[i].point;

                contactPoint = ContactPointsMagnitude();
                
                if (meshFilters != null && meshFilters.Length >= 1 && meshDeformation)
                    DamageMesh(impulse);

                if (wheels != null && wheels.Length >= 1 && wheelDamage)
                    DamageWheel(impulse);

                if (detachableParts != null && detachableParts.Length >= 1 && partDamage)
                    DamagePart(impulse);

                if (lights != null && lights.Length >= 1 && lightDamage)
                    DamageLight(impulse);

            }

        }

    }

    /// <summary>
    /// Raises the collision enter event.
    /// </summary>
    /// <param name="collision">Collision.</param>
    public void OnCollisionWithRay(RaycastHit hit, float impulse) {

        if (!carController)
            return;

        if (!carController.useDamage)
            return;

        if (impulse < minimumCollisionImpulse)
            impulse = 0f;

        if (impulse > 10f)
            impulse = 10f;

        if (impulse > 0f) {

            deformingNow = true;
            deformed = false;

            repairNow = false;
            repaired = false;

            //  First, we are getting all contact points.
            contactPoint = hit.point;
            
            if (meshFilters != null && meshFilters.Length >= 1 && meshDeformation)
                DamageMesh(impulse);

            if (wheels != null && wheels.Length >= 1 && wheelDamage)
                DamageWheel(impulse);

            if (detachableParts != null && detachableParts.Length >= 1 && partDamage)
                DamagePart(impulse);

            if (lights != null && lights.Length >= 1 && lightDamage)
                DamageLight(impulse);

        }

    }

    private Vector3 ContactPointsMagnitude() {

        Vector3 magnitude = Vector3.zero;

        for (int i = 0; i < contactPoints.Length; i++)
            magnitude += contactPoints[i];

        magnitude /= contactPoints.Length;

        return magnitude;

    }

    /// <summary>
    /// Finds closest vertex to the target point.
    /// </summary>
    /// <param name="trans"></param>
    /// <param name="mf"></param>
    /// <param name="point"></param>
    /// <returns></returns>
    public static Vector3 NearestVertex(Transform trans, MeshFilter mf, Vector3 point) {

        // Convert point to local space.
        point = trans.InverseTransformPoint(point);

        float minDistanceSqr = Mathf.Infinity;
        Vector3 nearestVertex = Vector3.zero;

        // Check all vertices to find nearest.
        foreach (Vector3 vertex in mf.mesh.vertices) {

            Vector3 diff = point - vertex;
            float distSqr = diff.sqrMagnitude;

            if (distSqr < minDistanceSqr) {

                minDistanceSqr = distSqr;
                nearestVertex = vertex;

            }

        }

        // Convert nearest vertex back to the world space.
        return trans.TransformPoint(nearestVertex);

    }

}
