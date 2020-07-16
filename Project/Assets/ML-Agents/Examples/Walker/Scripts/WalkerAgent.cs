using System;
using MLAgentsExamples;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgentsExamples;
using Unity.MLAgents.Sensors;
using BodyPart = Unity.MLAgentsExamples.BodyPart;
using Random = UnityEngine.Random;

public class WalkerAgent : Agent
{
    [Range(0, 15)]
    public float walkingSpeed = 15; //The walking speed to try and achieve
    float m_maxWalkingSpeed = 15; //The max walking speed
    public bool randomizeWalkSpeedEachEpisode;
    
    Vector3 m_WalkDir; //Direction to the target

    [Header("Target To Walk Towards")] [Space(10)]
    public TargetController target; //Target the agent will walk towards.

    [Header("Body Parts")] [Space(10)] public Transform hips;
    public Transform chest;
    public Transform spine;
    public Transform head;
    public Transform thighL;
    public Transform shinL;
    public Transform footL;
    public Transform thighR;
    public Transform shinR;
    public Transform footR;
    public Transform armL;
    public Transform forearmL;
    public Transform handL;
    public Transform armR;
    public Transform forearmR;
    public Transform handR;

    [Header("Orientation")] [Space(10)]
    //This will be used as a stabilized model space reference point for observations
    //Because ragdolls can move erratically during training, using a stabilized reference transform improves learning
    public OrientationCubeController orientationCube;

    JointDriveController m_JdController;

    EnvironmentParameters m_ResetParams;

    public override void Initialize()
    {
        orientationCube.UpdateOrientation(hips, target.transform);

        //Setup each body part
        m_JdController = GetComponent<JointDriveController>();
        m_JdController.SetupBodyPart(hips);
        m_JdController.SetupBodyPart(chest);
        m_JdController.SetupBodyPart(spine);
        m_JdController.SetupBodyPart(head);
        m_JdController.SetupBodyPart(thighL);
        m_JdController.SetupBodyPart(shinL);
        m_JdController.SetupBodyPart(footL);
        m_JdController.SetupBodyPart(thighR);
        m_JdController.SetupBodyPart(shinR);
        m_JdController.SetupBodyPart(footR);
        m_JdController.SetupBodyPart(armL);
        m_JdController.SetupBodyPart(forearmL);
        m_JdController.SetupBodyPart(handL);
        m_JdController.SetupBodyPart(armR);
        m_JdController.SetupBodyPart(forearmR);
        m_JdController.SetupBodyPart(handR);

        m_ResetParams = Academy.Instance.EnvironmentParameters;

        SetResetParameters();
    }

    /// <summary>
    /// Loop over body parts and reset them to initial conditions.
    /// </summary>
    public override void OnEpisodeBegin()
    {
        //Reset all of the body parts
        foreach (var bodyPart in m_JdController.bodyPartsDict.Values)
        {
            bodyPart.Reset(bodyPart);
        }

        //Random start rotation to help generalize
        transform.rotation = Quaternion.Euler(0, Random.Range(0.0f, 360.0f), 0);

        orientationCube.UpdateOrientation(hips, target.transform);

        rewardManager.ResetEpisodeRewards();
        
        walkingSpeed = randomizeWalkSpeedEachEpisode? Random.Range(0.0f, m_maxWalkingSpeed): walkingSpeed; //Random Walk Speed

        SetResetParameters();
    }

    /// <summary>
    /// Add relevant information on each body part to observations.
    /// </summary>
    public void CollectObservationBodyPart(BodyPart bp, VectorSensor sensor)
    {
        //GROUND CHECK
        sensor.AddObservation(bp.groundContact.touchingGround); // Is this bp touching the ground

        //Get velocities in the context of our orientation cube's space
        //Note: You can get these velocities in world space as well but it may not train as well.
        sensor.AddObservation(orientationCube.transform.InverseTransformDirection(bp.rb.velocity));
        sensor.AddObservation(orientationCube.transform.InverseTransformDirection(bp.rb.angularVelocity));

        //Get position relative to hips in the context of our orientation cube's space
        sensor.AddObservation(orientationCube.transform.InverseTransformDirection(bp.rb.position - hips.position));

        if (bp.rb.transform != hips && bp.rb.transform != handL && bp.rb.transform != handR)
        {
            sensor.AddObservation(bp.rb.transform.localRotation);
            sensor.AddObservation(bp.currentStrength / m_JdController.maxJointForceLimit);
        }
    }

    /// <summary>
    /// Loop over body parts to add them to observation.
    /// </summary>
    public override void CollectObservations(VectorSensor sensor)
    {
        
        sensor.AddObservation(walkingSpeed);
        sensor.AddObservation(Quaternion.FromToRotation(hips.forward, orientationCube.transform.forward));
        sensor.AddObservation(Quaternion.FromToRotation(head.forward, orientationCube.transform.forward));

        sensor.AddObservation(orientationCube.transform.InverseTransformPoint(target.transform.position));

        foreach (var bodyPart in m_JdController.bodyPartsList)
        {
            CollectObservationBodyPart(bodyPart, sensor);
        }
    }

    public override void OnActionReceived(float[] vectorAction)
    {
        var bpDict = m_JdController.bodyPartsDict;
        var i = -1;

        bpDict[chest].SetJointTargetRotation(vectorAction[++i], vectorAction[++i], vectorAction[++i]);
        bpDict[spine].SetJointTargetRotation(vectorAction[++i], vectorAction[++i], vectorAction[++i]);

        bpDict[thighL].SetJointTargetRotation(vectorAction[++i], vectorAction[++i], 0);
        bpDict[thighR].SetJointTargetRotation(vectorAction[++i], vectorAction[++i], 0);
        bpDict[shinL].SetJointTargetRotation(vectorAction[++i], 0, 0);
        bpDict[shinR].SetJointTargetRotation(vectorAction[++i], 0, 0);
        bpDict[footR].SetJointTargetRotation(vectorAction[++i], vectorAction[++i], vectorAction[++i]);
        bpDict[footL].SetJointTargetRotation(vectorAction[++i], vectorAction[++i], vectorAction[++i]);

        bpDict[armL].SetJointTargetRotation(vectorAction[++i], vectorAction[++i], 0);
        bpDict[armR].SetJointTargetRotation(vectorAction[++i], vectorAction[++i], 0);
        bpDict[forearmL].SetJointTargetRotation(vectorAction[++i], 0, 0);
        bpDict[forearmR].SetJointTargetRotation(vectorAction[++i], 0, 0);
        bpDict[head].SetJointTargetRotation(vectorAction[++i], vectorAction[++i], 0);

        //update joint strength settings
        bpDict[chest].SetJointStrength(vectorAction[++i]);
        bpDict[spine].SetJointStrength(vectorAction[++i]);
        bpDict[head].SetJointStrength(vectorAction[++i]);
        bpDict[thighL].SetJointStrength(vectorAction[++i]);
        bpDict[shinL].SetJointStrength(vectorAction[++i]);
        bpDict[footL].SetJointStrength(vectorAction[++i]);
        bpDict[thighR].SetJointStrength(vectorAction[++i]);
        bpDict[shinR].SetJointStrength(vectorAction[++i]);
        bpDict[footR].SetJointStrength(vectorAction[++i]);
        bpDict[armL].SetJointStrength(vectorAction[++i]);
        bpDict[forearmL].SetJointStrength(vectorAction[++i]);
        bpDict[armR].SetJointStrength(vectorAction[++i]);
        bpDict[forearmR].SetJointStrength(vectorAction[++i]);
    }

    void FixedUpdate()
    {
        UpdateRewards();
    }

    public float lookAtTargetReward; //reward for looking at the target
    public float matchSpeedReward; //reward for matching the desired walking speed.
    public float headHeightOverFeetReward; //reward for standing up straight-ish
    public float hurryUpReward = -1; //don't waste time
    public RewardManager rewardManager;
    public float bpVelPenaltyThisStep = 0;
    void UpdateRewards()
    {
        var cubeForward = orientationCube.transform.forward;
        orientationCube.UpdateOrientation(hips, target.transform);
        // Set reward for this step according to mixture of the following elements.
        // a. Match target speed
        //This reward will approach 1 if it matches and approach zero as it deviates
//        matchSpeedReward =
//            Mathf.Exp(-0.1f * (cubeForward * walkingSpeed -
//                               m_JdController.bodyPartsDict[hips].rb.velocity).sqrMagnitude);
//        var moveTowardsTargetReward = Vector3.Dot(cubeForward,
//            Vector3.ClampMagnitude(m_JdController.bodyPartsDict[hips].rb.velocity, maximumWalkingSpeed));
        // b. Rotation alignment with goal direction.
        lookAtTargetReward = Vector3.Dot(cubeForward, head.forward);
//        lookAtTargetReward =
//            Mathf.Exp(-0.1f * (cubeForward * walkingSpeed -
//                               m_JdController.bodyPartsDict[hips].rb.velocity).sqrMagnitude);
        // c. Encourage head height.
        headHeightOverFeetReward =
            (((head.position.y - footL.position.y) + (head.position.y - footR.position.y))/ 10); //Should normalize to ~1
//        AddReward(
//            +0.02f * moveTowardsTargetReward
//            + 0.01f * lookAtTargetReward
//            + 0.01f * headHeightOverFeetReward
//        );

//        rewardManager.UpdateReward("matchSpeed", matchSpeedReward);
//        rewardManager.UpdateReward("lookAtTarget", lookAtTargetReward);
//        rewardManager.UpdateReward("headHeightOverFeet", headHeightOverFeetReward);
//        rewardManager.UpdateReward("hurryUp", hurryUpReward/MaxStep);

//        //VELOCITY REWARDS
//        bpVelPenaltyThisStep = 0;
//        foreach (var item in m_JdController.bodyPartsList)
//        {
//            var velDelta = Mathf.Clamp(item.rb.velocity.magnitude - walkingSpeed, 0, 1);
//            bpVelPenaltyThisStep += velDelta;
//        }
//        rewardManager.UpdateReward("bpVel", bpVelPenaltyThisStep);
        Vector3 velSum = Vector3.zero;

        int counter = 0;
        avgVelValue = Vector3.zero;
        foreach (var item in m_JdController.bodyPartsList)
        {
            counter++;
//            velSum += item.rb.velocity;
//            velSum += Mathf.Clamp(item.rb.velocity.magnitude, 0, m_maxWalkingSpeed);
            velSum += Vector3.ClampMagnitude(item.rb.velocity, m_maxWalkingSpeed);
            avgVelValue = velSum/counter;
        }
        //This reward will approach 1 if it matches and approach zero as it deviates
        matchSpeedReward =
            Mathf.Exp(-0.1f * (cubeForward * walkingSpeed -
                               avgVelValue).sqrMagnitude);
        rewardManager.UpdateReward("productOfAllRewards", matchSpeedReward * lookAtTargetReward * headHeightOverFeetReward);
        
    }
    public Vector3 avgVelValue;
    
//    void FixedUpdate()
//    {
//        var cubeForward = orientationCube.transform.forward;
//        orientationCube.UpdateOrientation(hips, target.transform);
//        // Set reward for this step according to mixture of the following elements.
//        // a. Velocity alignment with goal direction.
//        var moveTowardsTargetReward = Vector3.Dot(cubeForward,
//            Vector3.ClampMagnitude(m_JdController.bodyPartsDict[hips].rb.velocity, maximumWalkingSpeed));
//        // b. Rotation alignment with goal direction.
//        var lookAtTargetReward = Vector3.Dot(cubeForward, head.forward);
//        // c. Encourage head height. //Should normalize to ~1
//        var headHeightOverFeetReward = 
//            ((head.position.y - footL.position.y) + (head.position.y - footR.position.y) / 10);
//        AddReward(
//            + 0.02f * moveTowardsTargetReward
//            + 0.02f * lookAtTargetReward
//            + 0.005f * headHeightOverFeetReward
//        );
//    }

    /// <summary>
    /// Agent touched the target
    /// </summary>
    public void TouchedTarget()
    {
        AddReward(1f);
    }

    public void SetTorsoMass()
    {
        m_JdController.bodyPartsDict[chest].rb.mass = m_ResetParams.GetWithDefault("chest_mass", 8);
        m_JdController.bodyPartsDict[spine].rb.mass = m_ResetParams.GetWithDefault("spine_mass", 8);
        m_JdController.bodyPartsDict[hips].rb.mass = m_ResetParams.GetWithDefault("hip_mass", 8);
    }

    public void SetResetParameters()
    {
        SetTorsoMass();
    }
}