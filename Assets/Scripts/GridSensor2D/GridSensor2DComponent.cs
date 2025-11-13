using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class GridSensor2DComponent : SensorComponent
{
    // dummy sensor only used for debug gizmo
        GridSensor2DBase m_DebugSensor;
        List<GridSensor2DBase> m_Sensors;
        SquareOverlapChecker m_BoxOverlapChecker;

        [HideInInspector, SerializeField]
        protected string m_SensorName = "GridSensor";
        /// <summary>
        /// Name of the generated GridSensor object.
        /// Note that changing this at runtime does not affect how the Agent sorts the sensors.
        /// </summary>
        public string SensorName
        {
            get { return m_SensorName; }
            set { m_SensorName = value; }
        }

        [SerializeField]
         Vector3 m_CellScale = new Vector3(1f, 0.01f, 1f);

        /// <summary>
        /// The scale of each grid cell.
        /// Note that changing this after the sensor is created has no effect.
        /// </summary>
        public Vector3 CellScale
        {
            get { return m_CellScale; }
            set { m_CellScale = value; }
        }

        [SerializeField]
         Vector3Int m_GridSize = new Vector3Int(16, 1, 16);
        /// <summary>
        /// The number of grid on each side.
        /// Note that changing this after the sensor is created has no effect.
        /// </summary>
        public Vector3Int GridSize
        {
            get { return m_GridSize; }
            set
            {
                if (value.y != 1)
                {
                    m_GridSize = new Vector3Int(value.x, 1, value.z);
                }
                else
                {
                    m_GridSize = value;
                }
            }
        }

        [SerializeField]
         bool m_RotateWithAgent = true;
        /// <summary>
        /// Rotate the grid based on the direction the agent is facing.
        /// </summary>
        public bool RotateWithAgent
        {
            get { return m_RotateWithAgent; }
            set { m_RotateWithAgent = value; }
        }

        [SerializeField]
         GameObject m_AgentGameObject;
        /// <summary>
        /// The reference of the root of the agent. This is used to disambiguate objects with
        /// the same tag as the agent. Defaults to current GameObject.
        /// </summary>
        public GameObject AgentGameObject
        {
            get { return (m_AgentGameObject == null ? gameObject : m_AgentGameObject); }
            set { m_AgentGameObject = value; }
        }

        [SerializeField]
         string[] m_DetectableTags;
        /// <summary>
        /// List of tags that are detected.
        /// Note that changing this after the sensor is created has no effect.
        /// </summary>
        public string[] DetectableTags
        {
            get { return m_DetectableTags; }
            set { m_DetectableTags = value; }
        }

        [SerializeField]
         LayerMask m_ColliderMask;
        /// <summary>
        /// The layer mask.
        /// </summary>
        public LayerMask ColliderMask
        {
            get { return m_ColliderMask; }
            set { m_ColliderMask = value; }
        }

        [SerializeField]
         int m_MaxColliderBufferSize = 500;
        /// <summary>
        /// The absolute max size of the Collider buffer used in the non-allocating Physics calls.  In other words
        /// the Collider buffer will never grow beyond this number even if there are more Colliders in the Grid Cell.
        /// Note that changing this after the sensor is created has no effect.
        /// </summary>
        public int MaxColliderBufferSize
        {
            get { return m_MaxColliderBufferSize; }
            set { m_MaxColliderBufferSize = value; }
        }

        [SerializeField]
         int m_InitialColliderBufferSize = 4;
        /// <summary>
        /// The Estimated Max Number of Colliders to expect per cell.  This number is used to
        /// pre-allocate an array of Colliders in order to take advantage of the OverlapBoxNonAlloc
        /// Physics API.  If the number of colliders found is >= InitialColliderBufferSize the array
        /// will be resized to double its current size.  The hard coded absolute size is 500.
        /// Note that changing this after the sensor is created has no effect.
        /// </summary>
        public int InitialColliderBufferSize
        {
            get { return m_InitialColliderBufferSize; }
            set { m_InitialColliderBufferSize = value; }
        }

        [SerializeField]
         Color[] m_DebugColors;
        /// <summary>
        /// Array of Colors used for the grid gizmos.
        /// </summary>
        public Color[] DebugColors
        {
            get { return m_DebugColors; }
            set { m_DebugColors = value; }
        }

        [SerializeField]
         float m_GizmoYOffset = 0f;
        /// <summary>
        /// The height of the gizmos grid.
        /// </summary>
        public float GizmoYOffset
        {
            get { return m_GizmoYOffset; }
            set { m_GizmoYOffset = value; }
        }

        [SerializeField]
         bool m_ShowGizmos = false;
        /// <summary>
        /// Whether to show gizmos or not.
        /// </summary>
        public bool ShowGizmos
        {
            get { return m_ShowGizmos; }
            set { m_ShowGizmos = value; }
        }

        [SerializeField]
         SensorCompressionType m_CompressionType = SensorCompressionType.PNG;
        /// <summary>
        /// The compression type to use for the sensor.
        /// </summary>
        public SensorCompressionType CompressionType
        {
            get { return m_CompressionType; }
            set { m_CompressionType = value; UpdateSensor(); }
        }

        [SerializeField]
        [Range(1, 50)]
        [Tooltip("Number of frames of observations that will be stacked before being fed to the neural network.")]
         int m_ObservationStacks = 1;
        /// <summary>
        /// Whether to stack previous observations. Using 1 means no previous observations.
        /// Note that changing this after the sensor is created has no effect.
        /// </summary>
        public int ObservationStacks
        {
            get { return m_ObservationStacks; }
            set { m_ObservationStacks = value; }
        }

        /// <inheritdoc/>
        public override ISensor[] CreateSensors()
        {
            int teamId = m_AgentGameObject.GetComponent<CharacterStatus>().TeamId;
            m_BoxOverlapChecker = new SquareOverlapChecker(
                m_CellScale,
                m_GridSize,
                m_RotateWithAgent,
                m_ColliderMask,
                gameObject,
                AgentGameObject,
                m_DetectableTags,
                m_InitialColliderBufferSize,
                m_MaxColliderBufferSize
            );

            // debug data is positive int value and will trigger data validation exception if SensorCompressionType is not None.
            m_DebugSensor = new GridSensor2DBase("DebugGridSensor", m_CellScale, m_GridSize, m_DetectableTags, SensorCompressionType.None, teamId);
            m_BoxOverlapChecker.RegisterDebugSensor(m_DebugSensor);

            m_Sensors = GetGridSensors().ToList();
            if (m_Sensors == null || m_Sensors.Count < 1)
            {
                throw new UnityAgentsException("GridSensorComponent received no sensors. Specify at least one observation type (OneHot/Counting) to use grid sensors." +
                    "If you're overriding GridSensorComponent.GetGridSensors(), return at least one grid sensor.");
            }

            // Only one sensor needs to reference the boxOverlapChecker, so that it gets updated exactly once
            m_Sensors[0].m_BoxOverlapChecker = m_BoxOverlapChecker;
            foreach (var sensor in m_Sensors)
            {
                m_BoxOverlapChecker.RegisterSensor(sensor);
            }

            if (ObservationStacks != 1)
            {
                var sensors = new ISensor[m_Sensors.Count];
                for (var i = 0; i < m_Sensors.Count; i++)
                {
                    sensors[i] = new StackingSensor(m_Sensors[i], ObservationStacks);
                }
                return sensors;
            }
            else
            {
                return m_Sensors.ToArray();
            }
        }

        /// <summary>
        /// Get an array of GridSensors to be added in this component.
        /// Override this method and return custom GridSensor implementations.
        /// </summary>
        /// <returns>Array of grid sensors to be added to the component.</returns>
        public virtual GridSensor2DBase[] GetGridSensors()
        {
            int teamId = m_AgentGameObject.GetComponent<CharacterStatus>().TeamId;
            List<GridSensor2DBase> sensorList = new List<GridSensor2DBase>();
            var sensor = new GridSensor2DBase(m_SensorName + "-OneHot", m_CellScale, m_GridSize, m_DetectableTags, m_CompressionType, teamId);
            sensorList.Add(sensor);
            return sensorList.ToArray();
        }

        /// <summary>
        /// Update fields that are safe to change on the Sensor at runtime.
        /// </summary>
        public void UpdateSensor()
        {
            if (m_Sensors != null)
            {
                m_BoxOverlapChecker.RotateWithAgent = m_RotateWithAgent;
                m_BoxOverlapChecker.ColliderMask = m_ColliderMask;
                foreach (var sensor in m_Sensors)
                {
                    sensor.CompressionType = m_CompressionType;
                }
            }
        }

        void OnDrawGizmos()
        {
            if (m_ShowGizmos)
            {
                if (m_BoxOverlapChecker == null || m_DebugSensor == null)
                {
                    return;
                }

                m_DebugSensor.ResetPerceptionBuffer();
                m_BoxOverlapChecker.UpdateGizmo();
                var cellColors = m_DebugSensor.PerceptionBuffer;
                var rotation = m_BoxOverlapChecker.GetGridRotation();

                var scale = new Vector3(m_CellScale.x, 0.1f, m_CellScale.z);
                var gizmoYOffset = new Vector3(0, m_GizmoYOffset, 0);
                var oldGizmoMatrix = Gizmos.matrix;
                for (var i = 0; i < m_DebugSensor.PerceptionBuffer.Length; i++)
                {
                    int layerIdx = i % m_DebugSensor.GetCellObservationSize();
                    var cellPosition = m_BoxOverlapChecker.GetCellGlobalPosition(i / m_DebugSensor.GetCellObservationSize());
                    cellPosition.z += layerIdx * 0.2f;
                    var cubeTransform = Matrix4x4.TRS(cellPosition + gizmoYOffset, rotation, scale);
                    Gizmos.matrix = oldGizmoMatrix * cubeTransform;
                    
                    var colorIndex = i % m_DebugSensor.GetCellObservationSize();
                    var debugRayColor = Color.white;
                    debugRayColor.a = 0.01f;
                    if (colorIndex > -1 && m_DebugColors.Length > colorIndex && m_DebugSensor.PerceptionBuffer[i] != 0)
                    {
                        debugRayColor = m_DebugColors[(int)colorIndex];
                        debugRayColor.a = 0.7f;
                    }
                    Gizmos.color = new Color(debugRayColor.r, debugRayColor.g, debugRayColor.b, debugRayColor.a);
                    Vector3 cubeScale = Vector3.one;
                    Vector3 cubePos = Vector3.zero;
                    if (m_DebugSensor.PerceptionBuffer[i] != 0)
                    {
                        if (layerIdx == 2 || layerIdx == 5)
                        {
                            cubeScale.x = Mathf.Abs(m_DebugSensor.PerceptionBuffer[i]) / 2f;
                            cubeScale.z = 0.1f;
                            cubePos.x = m_DebugSensor.PerceptionBuffer[i] / 2f;
                        }

                        if (layerIdx == 3 || layerIdx == 6)
                        {
                            cubeScale.z = Mathf.Abs(m_DebugSensor.PerceptionBuffer[i]) / 2f;
                            cubeScale.x = 0.1f;
                            cubePos.z = m_DebugSensor.PerceptionBuffer[i] / 2f;
                        }
                    }

                    if (m_DebugSensor.PerceptionBuffer[i] != 0)
                    {
                        Gizmos.DrawCube(cubePos, cubeScale);
                    }
                }

                Gizmos.matrix = oldGizmoMatrix;
            }
        }
}
