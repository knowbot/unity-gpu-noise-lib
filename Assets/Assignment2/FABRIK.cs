using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AfGD.Assignment2
{
    public class FABRIK : MonoBehaviour
    {
        [Tooltip("the joints that we are controlling")]
        public Transform[] joints;

        [Tooltip("target that our end effector is trying to reach")]
        public Transform target;

        [Tooltip("error tolerance, will stop updating after distance between end effector and target is smaller than tolerance.")]
        [Range(.01f, .2f)]
        public float tolerance = 0.05f;

        [Tooltip("maximum number of iterations before we follow to the next frame")]
        [Range(1, 100)]
        public int maxIterations = 20;

        [Tooltip("rotation constraint. " +
        	"Instead of an elipse with 4 rotation limits, " +
        	"we use a circle with a single rotation limit. " +
        	"Implementation will be a lot simpler than in the paper.")]
        [Range(0f, 180f)]
        public float rotationLimit = 45f;

        // distances/lengths between joints.
        private float[] distances;
        // total length of the system
        private float chainLength;
        // distance between root and target;
        private float distFromRootToTarget;
        private float distFromEndEffectorToTarget;
        private Vector3 rootInitialPosition;
        private Vector3[] jointStartDirection;
        private Quaternion[] startRotation;


        private void Solve()
        {
            // TODO: YOUR IMPLEMENTATION HERE
            // FEEL FREE TO CREATE HELPER FUNCTIONS
            distFromRootToTarget = (target.position - joints[0].position).magnitude;
            //Check whether the target is within reach
            if(distFromRootToTarget > chainLength)
            {
                //target is unreachable
                for(int i=0; i< joints.Length - 1; i++)
                {
                    var direction = target.position - joints[0].position;
                    //find the distance r between the target t and the joint position pi
                    float r = (target.position - joints[i].position).magnitude;
                    float lambda = distances[i] / r;
                    //find the new joint positions pi
                    joints[i + 1].position = (1 - lambda) * joints[i].position + lambda * target.position;
                }
            }
            else
            {
                //the target is reachable; thus, set as b the initial position of the joint p1
                //Transform b = joints[0];
                rootInitialPosition = joints[0].position;
                //Check whether the distance between the end effector Pn and the target T is greater than a tolerance;
                distFromEndEffectorToTarget = (target.position - joints[joints.Length - 1].position).magnitude;
                while (distFromEndEffectorToTarget > tolerance && maxIterations > 0)
                {
                    //Stage 1: Forward Reaching
                    ForwardReaching();
                    //Stage 2: Backward Reaching
                    BackwardReaching();
                    distFromEndEffectorToTarget = (target.position - joints[joints.Length - 1].position).magnitude;
                    maxIterations--;
                }
                
            }
            //2.3. Set joint limitations
            /*for (int i = 1; i < joints.Length - 1; i++)
            {
                joints[i + 1].position = RotationConstraints(joints[i - 1].position, joints[i].position, joints[i + 1].position);
            }*/
            //2.2. Set joint rotations
            for (int i = 0; i < joints.Length -1; i++)
            {
                var targetRotation = Quaternion.FromToRotation(jointStartDirection[i], joints[i + 1].position - joints[i].position);
                Debug.Log("jointStartDirection" + i + " " + jointStartDirection[i]);
                joints[i].rotation = targetRotation * startRotation[i];
                Debug.Log("jointRotation" + i + " " + joints[i].rotation);
            }
            maxIterations = 20;
        }

        private void ForwardReaching()
        {

            //2.3. Set joint limitations
            for (int i = joints.Length - 1; i > 1; i--)
            {
                joints[i].position = RotationConstraints(joints[i - 2].position, joints[i-1].position, joints[i].position);
                //Debug.Log("Forward - Joint number: " + i + " " + joints[i].position);
            }

            //2.1. Set the end effector Pn as target T
            joints[joints.Length - 1].position = target.position;
            for(int i = joints.Length - 2; i >= 0; i--)
            {
                //find the distance r between the new joint position P(i+1) and the joint pi
                
                float r = (joints[i + 1].position - joints[i].position).magnitude;
                float lambda = distances[i] / r;
                //find the new joint positions Pi
                joints[i].position = (1 - lambda) * joints[i + 1].position + lambda * joints[i].position;
            }
        }

        private void BackwardReaching()
        {
            //2.3. Set joint limitations
            for (int i = 2; i <= joints.Length - 1; i++)
            {
                joints[i].position = RotationConstraints(joints[i - 2].position, joints[i - 1].position, joints[i].position);
                //Debug.Log("Backward - Joint number: " + i + " " + joints[i].position);
            }
            //2.1. set the root p1 its initial position
            joints[0].position = rootInitialPosition;
            for (int i = 0; i < joints.Length - 1; i++)
            {
                //find the distance r between the new joint position Pi and the joint P(i+1)
                float r = (joints[i + 1].position - joints[i].position).magnitude;
                float lambda = distances[i] / r;
                //find the new joint positions Pi
                joints[i + 1].position = (1 - lambda) * joints[i].position + lambda * joints[i + 1].position;
            }
        }

        //Video tutorial of the math behind the Rotation Constraints by Henrique: https://app.vidgrid.com/view/ewgQYDgTZYnp/?sr=YHlIMmSgWzdY
        private Vector3 RotationConstraints(Vector3 prevJoint_Position, Vector3 currentJoint_Position, Vector3 nextJoint_Position)
        {
            //Vector from prevJoint to currentJoint, this vector has no notion of position, so the tale can be the current Joint
            Vector3 l = currentJoint_Position - prevJoint_Position;
            //Vector from currentJoint to nextJoint
            Vector3 l_n = nextJoint_Position - currentJoint_Position;

            //if the nextJoint target position is within the rotation Limit we can exit from this function, because we don't have to perform any corrections
            if (Vector3.Angle(l, l_n) < rotationLimit)
                return nextJoint_Position;

            //Projection of l_n on L - dot product of (l_n, l.normalized) * l.normalized, where the first mart gives a magnitude of the projection, the second gives a direction.
            Vector3 O = Vector3.Project(l_n, l);

            //If the angle between l and l_n is greater than 90 degrees -> l_n points toward the previousJoint position
            //If dot product of O and l is <0 than the angle is greater than 90 degrees.
            if (Vector3.Dot(O, l) < 0)
            {
                //Mirror O
                O = -O;
                //reflect l_n to point toward direction l
                l_n = Vector3.Reflect(l_n, l);
            }

            //This is a Position from the current joint into the direction of the l vector in the distance of the projection of l_n on l. This is the O in the paper.
            Vector3 P_O = currentJoint_Position + O;
            //The direction of the vector from P_O the the nextJoint position.
            Vector3 d = (nextJoint_Position - P_O).normalized;

            //We have to convert the rotation limit from degrees to radians
            float ritationLimit_in_rad = rotationLimit * Mathf.Deg2Rad;
            //The radius of the circle of a cone at point P_O formed by the rotationLimit angle
            //if Joint limit is > 90 degrees we will get a negative tangent value, so we would need the abs of the radius
            var r = Mathf.Abs(O.magnitude * Mathf.Tan(ritationLimit_in_rad));

            //Reposition the nextJoint Position withing the constraint angle
            nextJoint_Position = P_O + r * d;
            return nextJoint_Position;
        }

        // Start is called before the first frame update
        void Start()
        {
            // pre-compute segment lenghts and total length of the chain
            // we assume that the segment/bone length is constant during execution
            var current = transform;

            jointStartDirection = new Vector3[joints.Length - 1];
            startRotation = new Quaternion[joints.Length - 1];

            distances = new float[joints.Length-1];
            chainLength = 0;
            // If we have N joints, then there are N-1 segment/bone lengths connecting these joints
            for (int i = 0; i < joints.Length - 1; i++)
            {
                distances[i] = (joints[i + 1].position - joints[i].position).magnitude;
                chainLength += distances[i]; //d1+d2+...+dn-1

                jointStartDirection[i] = (joints[i + 1].position - joints[i].position).normalized;
                startRotation[i] = joints[i].rotation;
            }

            //jointStartDirection[joints.Length - 1] 
        }

        void Update()
        {
            Solve();
            for (int i = 1; i < joints.Length - 1; i++)
            {
                DebugJointLimit(joints[i], joints[i - 1], rotationLimit, 2);
            }
        }

        /// <summary>
        /// Helper function to draw the joint limit in the editor
        /// The drawing migh not make sense if you did not complete the 
        /// second task in the assignment (joint rotations)
        /// </summary>
        /// <param name="tr">current joint</param>
        /// <param name="trPrev">previous joint</param>
        /// <param name="angle">angle limit in degrees</param>
        /// <param name="scale"></param>
        void DebugJointLimit(Transform tr, Transform trPrev, float angle, float scale = 1)
        {
            float angleRad = Mathf.Deg2Rad * angle;
            float cosAngle = Mathf.Cos(angleRad);
            float sinAngle = Mathf.Sin(angleRad);
            int steps = 36;
            float stepSize = 360f / steps;
            // steps is the number of line segments used to draw the cone
            for (int i = 0; i < steps; i++)
            {
                float twistRad = Mathf.Deg2Rad * i * stepSize;
                Vector3 vec = new Vector3(cosAngle, 0, 0);
                vec.y = Mathf.Cos(twistRad) * sinAngle;
                vec.z = Mathf.Sin(twistRad) * sinAngle;
                vec = trPrev.rotation * vec;
                
                twistRad = Mathf.Deg2Rad * (i+1) * stepSize;
                Vector3 vec2 = new Vector3(cosAngle, 0, 0);
                vec2.y = Mathf.Cos(twistRad) * sinAngle;
                vec2.z = Mathf.Sin(twistRad) * sinAngle;
                vec2 = trPrev.rotation * vec2;

                Debug.DrawLine(tr.position, tr.position + vec * scale, Color.white);
                Debug.DrawLine(tr.position + vec * scale, tr.position + vec2 * scale, Color.white);
            }
        }
    }

}