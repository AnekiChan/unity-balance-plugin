using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace BalancePlugin
{
    public enum EditorMode
    {
        Select,
        Connect
    }

    public class BalancingWindow : EditorWindow
    {
        private BalancingData _data;
        private EditorMode _currentMode = EditorMode.Select;
        private Vector2 _scrollOffset;
        private float _zoom = 1f;
        private bool _showGrid = true;
        private const float GridSize = 20f;
        private const float NodeWidth = 100f;
        private const float NodeHeight = 40f;
        private const float ConnectionPointRadius = 8f;

        private BalancingNode _draggedNode;
        private Vector2 _dragOffset;
        private bool _isDraggingConnection;
        private BalancingNode _connectionFromNode;
        private Vector2 _connectionEndPoint;
        private BalancingNode _connectionStartNode;
        private bool _isConnectingViaNode;

        private BalancingNode _selectedNode;
        private NodeInspectorWindow _inspectorWindow;
        private bool _inspectorWindowOpen;
        private bool _isPanning;
        private Vector2 _lastMousePos;

        [MenuItem("Tools/Balancing Window")]
        public static void ShowWindow()
        {
            GetWindow<BalancingWindow>("Balancing");
        }

        private void OnGUI()
        {
            Event e = Event.current;

            if (_data == null)
            {
                _data = (BalancingData)EditorGUILayout.ObjectField(_data, typeof(BalancingData), false);
                return;
            }

            GUILayout.BeginHorizontal();
            _data = (BalancingData)EditorGUILayout.ObjectField(_data, typeof(BalancingData), false);
            if (GUILayout.Button("Create"))
            {
                CreateNewData();
            }
            _showGrid = GUILayout.Toggle(_showGrid, "Show Grid");
            GUILayout.Label("Zoom: " + _zoom.ToString("F1"));
            if (GUILayout.Button(_currentMode == EditorMode.Select ? "Connect" : "Select", GUILayout.Width(60)))
            {
                _currentMode = _currentMode == EditorMode.Select ? EditorMode.Connect : EditorMode.Select;
            }
            GUILayout.EndHorizontal();

            if (_showGrid)
                DrawGrid();

            DrawConnections();
            DrawNodes();

            ProcessInput(e);
            
            HandleMouseCapture();
        }
        
        private void HandleMouseCapture()
        {
            if (_isPanning)
            {
                Repaint();
            }
        }

        private void CreateNewData()
        {
            try
            {
                BalancingData newData = CreateInstance<BalancingData>();
                newData.hideFlags = HideFlags.None;

                // string path = "Assets/Balance Plugin/SOs/BalancingData.asset";
                string basePath = "Assets/Balance Plugin/SOs/BalancingData";
                string extension = ".asset";
                string path = basePath + extension;

                int counter = 1;
                while (AssetDatabase.LoadAssetAtPath<BalancingData>(path) != null)
                {
                    path = basePath + counter + extension;
                    counter++;
                }

                AssetDatabase.CreateAsset(newData, path);
                AssetDatabase.SaveAssets();

                _data = newData;
                EditorGUIUtility.PingObject(newData);
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError("CreateNewData: " + ex.Message + "\n" + ex.StackTrace);
            }
        }

        private void ProcessInput(Event e)
        {
            switch (e.type)
            {
                case EventType.ScrollWheel:
                    if (e.delta.y != 0)
                    {
                        _zoom = Mathf.Clamp(_zoom - e.delta.y * 0.1f, 0.1f, 3f);
                        e.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (e.button == 0)
                    {
                        if (_draggedNode != null)
                        {
                            Vector2 mouseWorld = (e.mousePosition + _scrollOffset) / _zoom;
                            _draggedNode.Position = mouseWorld - _dragOffset;
                            GUI.changed = true;
                            Repaint();
                            e.Use();
                        }
                        else if (_isDraggingConnection)
                        {
                            _connectionEndPoint = e.mousePosition;
                            GUI.changed = true;
                            Repaint();
                            e.Use();
                        }
                        else if (_isConnectingViaNode)
                        {
                            _connectionEndPoint = e.mousePosition;
                            GUI.changed = true;
                            Repaint();
                            e.Use();
                        }
                    }
                    else if (_isPanning)
                    {
                        Vector2 delta = e.mousePosition - _lastMousePos;
                        _scrollOffset -= delta;
                        _lastMousePos = e.mousePosition;
                        Repaint();
                        e.Use();
                    }
                    break;

                case EventType.MouseDown:
                    if (_data != null)
                    {
                        if (e.button == 0)
                        {
                            BalancingNode clickedNode = GetNodeAtMouse();
                            if (clickedNode != null)
                            {
                                if (_currentMode == EditorMode.Connect)
                                {
                                    _isConnectingViaNode = true;
                                    _connectionStartNode = clickedNode;
                                    _connectionEndPoint = e.mousePosition;
                                }
                                else
                                {
                                    _draggedNode = clickedNode;
                                    _dragOffset = (e.mousePosition + _scrollOffset) / _zoom - clickedNode.Position;
                                    _selectedNode = clickedNode;
                                    _inspectorWindow?.SetNode(_selectedNode);
                                    OpenInspector();
                                }
                            }
                            else if (_currentMode == EditorMode.Select && GetConnectionPointAtMouse() != null)
                            {
                                _isDraggingConnection = true;
                                _connectionFromNode = GetConnectionPointAtMouse();
                                _connectionEndPoint = e.mousePosition;
                            }
                            else
                            {
                                _selectedNode = null;
                                _inspectorWindow?.SetNode(null);
                            }
                            e.Use();
                        }
                        else if (e.button == 1)
                        {
                            Vector2 mousePos = e.mousePosition / _zoom;
                            BalancingNode clickedNode = GetNodeAtMouse();
                            if (clickedNode != null)
                            {
                                ShowNodeContextMenu(clickedNode);
                            }
                            else
                            {
                                NodeConnection clickedConn = GetConnectionAtMouse();
                                if (clickedConn != null)
                                {
                                    ShowConnectionContextMenu(clickedConn);
                                }
                                else
                                {
                                    ShowCreateNodeMenu(mousePos);
                                }
                            }
                            e.Use();
                        }
                        else if (e.button == 2)
                        {
                            _isPanning = true;
                            _lastMousePos = e.mousePosition;
                            e.Use();
                        }
                    }
                    break;

                

                case EventType.MouseUp:
                    if (e.button == 0)
                    {
                        if (_isDraggingConnection && _data != null)
                        {
                            BalancingNode targetNode = GetNodeAtMouse();
                            if (targetNode != null && targetNode != _connectionFromNode)
                            {
                                CreateConnection(_connectionFromNode, targetNode);
                            }
                            _isDraggingConnection = false;
                            _connectionFromNode = null;
                        }
                        else if (_isConnectingViaNode && _connectionStartNode != null && _data != null)
                        {
                            BalancingNode targetNode = GetNodeAtMouse();
                            if (targetNode != null && targetNode != _connectionStartNode)
                            {
                                CreateConnection(_connectionStartNode, targetNode);
                            }
                            _isConnectingViaNode = false;
                            _connectionStartNode = null;
                        }
                        _draggedNode = null;
                        e.Use();
                    }
                    else if (e.button == 2)
                    {
                        _isPanning = false;
                        e.Use();
                    }
                    break;

                case EventType.KeyDown:
                    if (e.keyCode == KeyCode.Delete && _selectedNode != null && _data != null)
                    {
                        string deletedNodeId = _selectedNode.NodeId;
                        foreach (var node in _data.Nodes)
                        {
                            node.InputNodeIds.Remove(deletedNodeId);
                            node.OutputNodeIds.Remove(deletedNodeId);
                        }
                        _data.Nodes.Remove(_selectedNode);
                        _data.Connections.RemoveAll(c => c.FromNodeId == deletedNodeId || c.ToNodeId == deletedNodeId);
                        _selectedNode = null;
                        e.Use();
                    }
                    break;
            }
        }

        private Vector2 GetMouseWorldPosition()
        {
            return (Event.current.mousePosition + _scrollOffset) / _zoom;
        }

        private void DrawGrid()
        {
            Handles.BeginGUI();
            Handles.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);

            float scaledGridSize = GridSize * _zoom;
            Vector2 gridOffset = -_scrollOffset * _zoom;
            gridOffset.x = gridOffset.x % scaledGridSize;
            gridOffset.y = gridOffset.y % scaledGridSize;

            for (float x = gridOffset.x; x < position.width; x += scaledGridSize)
            {
                Handles.DrawLine(new Vector2(x, 0), new Vector2(x, position.height));
            }
            for (float y = gridOffset.y; y < position.height; y += scaledGridSize)
            {
                Handles.DrawLine(new Vector2(0, y), new Vector2(position.width, y));
            }

            Handles.EndGUI();
        }

        private void DrawNodes()
        {
            if (_data == null || _data.Nodes == null)
                return;

            foreach (var node in _data.Nodes)
            {
                Vector2 pos = (node.Position - _scrollOffset) * _zoom;
                Rect nodeRect = new Rect(pos.x, pos.y, NodeWidth * _zoom, NodeHeight * _zoom);

                Color nodeColor = node.NodeColor;
                Color borderColor = node == _selectedNode ? Color.white : nodeColor;
                float borderWidth = node == _selectedNode ? 3f : 2f;

                DrawNodeBackground(nodeRect, borderColor, borderWidth);

                GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
                labelStyle.alignment = TextAnchor.MiddleCenter;
                labelStyle.fontSize = Mathf.RoundToInt(12 * _zoom);
                labelStyle.normal.textColor = Color.white;

                GUI.Label(nodeRect, node.DisplayName, labelStyle);

                Vector2 inputDotPos = new Vector2(pos.x, pos.y + NodeHeight * _zoom / 2);
                Vector2 outputDotPos = new Vector2(pos.x + NodeWidth * _zoom, pos.y + NodeHeight * _zoom / 2);

                Handles.BeginGUI();
                if (_currentMode == EditorMode.Connect)
                {
                    Handles.color = node == _connectionStartNode ? Color.green : Color.yellow;
                }
                else
                {
                    Handles.color = nodeColor;
                }
                Handles.DrawSolidDisc(inputDotPos, Vector3.forward, ConnectionPointRadius * _zoom);
                Handles.color = nodeColor;
                Handles.DrawSolidDisc(outputDotPos, Vector3.forward, ConnectionPointRadius * _zoom);
                Handles.EndGUI();
            }
        }

        private void DrawNodeBackground(Rect rect, Color borderColor, float borderWidth)
        {
            EditorGUI.DrawRect(rect, new Color(0.1f, 0.1f, 0.1f));
            EditorGUI.DrawRect(new Rect(rect.x - borderWidth, rect.y - borderWidth, rect.width + borderWidth * 2, borderWidth), borderColor);
            EditorGUI.DrawRect(new Rect(rect.x - borderWidth, rect.y + rect.height, rect.width + borderWidth * 2, borderWidth), borderColor);
            EditorGUI.DrawRect(new Rect(rect.x - borderWidth, rect.y, borderWidth, rect.height), borderColor);
            EditorGUI.DrawRect(new Rect(rect.x + rect.width, rect.y, borderWidth, rect.height), borderColor);
        }

        private void CreateConnection(BalancingNode fromNode, BalancingNode toNode)
        {
            if (fromNode.OutputNodeIds.Contains(toNode.NodeId))
                return;

            fromNode.OutputNodeIds.Add(toNode.NodeId);
            toNode.InputNodeIds.Add(fromNode.NodeId);

            _data.Connections.Add(new NodeConnection
            {
                FromNodeId = fromNode.NodeId,
                ToNodeId = toNode.NodeId
            });
        }

        private void DrawConnections()
        {
            if (_data == null || _data.Connections == null)
                return;

            Handles.BeginGUI();

            foreach (var conn in _data.Connections)
            {
                BalancingNode fromNode = _data.Nodes.Find(n => n.NodeId == conn.FromNodeId);
                BalancingNode toNode = _data.Nodes.Find(n => n.NodeId == conn.ToNodeId);

                if (fromNode == null || toNode == null) continue;

                Vector2 fromPos = (fromNode.Position - _scrollOffset + new Vector2(NodeWidth, NodeHeight / 2)) * _zoom;
                Vector2 toPos = (toNode.Position - _scrollOffset + new Vector2(0, NodeHeight / 2)) * _zoom;

                DrawArrow(fromPos, toPos);
            }

            if (_isDraggingConnection && _connectionFromNode != null)
            {
                Vector2 fromPos = (_connectionFromNode.Position - _scrollOffset + new Vector2(NodeWidth, NodeHeight / 2)) * _zoom;
                DrawArrow(fromPos, _connectionEndPoint);
            }

            if (_isConnectingViaNode && _connectionStartNode != null)
            {
                Vector2 fromPos = (_connectionStartNode.Position - _scrollOffset + new Vector2(0, NodeHeight / 2)) * _zoom;
                DrawArrow(fromPos, _connectionEndPoint);
            }

            Handles.EndGUI();
        }

        private void DrawArrow(Vector2 from, Vector2 to)
        {
            Handles.color = Color.white;
            Handles.DrawLine(from, to);

            Vector2 dir = (to - from);
            if (dir.sqrMagnitude < 0.001f)
                return;

            dir = dir.normalized;
            Vector2 perpendicular = new Vector2(-dir.y, dir.x);
            float arrowSize = 10f * _zoom;

            Handles.DrawLine(to, to - dir * arrowSize + perpendicular * arrowSize * 0.5f);
            Handles.DrawLine(to, to - dir * arrowSize - perpendicular * arrowSize * 0.5f);
        }

        private BalancingNode GetNodeAtMouse()
        {
            if (_data == null || _data.Nodes == null)
                return null;

            Vector2 mousePos = (Event.current.mousePosition + _scrollOffset) / _zoom;
            foreach (var node in _data.Nodes)
            {
                Rect nodeRect = new Rect(node.Position.x, node.Position.y, NodeWidth, NodeHeight);
                if (nodeRect.Contains(mousePos))
                    return node;
            }
            return null;
        }

        private BalancingNode GetConnectionPointAtMouse()
        {
            if (_data == null || _data.Nodes == null)
                return null;

            Vector2 mousePos = (Event.current.mousePosition + _scrollOffset) / _zoom;

            foreach (var n in _data.Nodes)
            {
                Vector2 outputPos = n.Position + new Vector2(NodeWidth, NodeHeight / 2);

                if (Vector2.Distance(mousePos, outputPos) < ConnectionPointRadius + 5)
                {
                    return n;
                }
            }
            return null;
        }

        private NodeConnection GetConnectionAtMouse()
        {
            if (_data == null || _data.Connections == null)
                return null;

            Vector2 mousePos = (Event.current.mousePosition + _scrollOffset) / _zoom;

            foreach (var conn in _data.Connections)
            {
                BalancingNode fromNode = _data.Nodes.Find(n => n.NodeId == conn.FromNodeId);
                BalancingNode toNode = _data.Nodes.Find(n => n.NodeId == conn.ToNodeId);

                if (fromNode == null || toNode == null)
                    continue;

                Vector2 fromPos = fromNode.Position + new Vector2(NodeWidth, NodeHeight / 2);
                Vector2 toPos = toNode.Position + new Vector2(0, NodeHeight / 2);

                if (DistanceToSegment(mousePos, fromPos, toPos) < 10f)
                    return conn;
            }
            return null;
        }

        private float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 pa = point - a, ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude;
        }

        private void ShowConnectionContextMenu(NodeConnection conn)
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Delete Connection"), false, () =>
            {
                var fromNode = _data.Nodes.Find(n => n.NodeId == conn.FromNodeId);
                var toNode = _data.Nodes.Find(n => n.NodeId == conn.ToNodeId);

                if (fromNode != null)
                {
                    fromNode.OutputNodeIds.Remove(conn.ToNodeId);
                }
                if (toNode != null)
                {
                    toNode.InputNodeIds.Remove(conn.FromNodeId);
                }

                _data.Connections.Remove(conn);
            });
            menu.ShowAsContext();
        }

        private void ShowCreateNodeMenu(Vector2 worldPos)
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Source"), false, () => CreateNode<SourceNode>(worldPos));
            menu.AddItem(new GUIContent("Drain"), false, () => CreateNode<DrainNode>(worldPos));
            menu.AddItem(new GUIContent("Pool"), false, () => CreateNode<PoolNode>(worldPos));
            menu.AddItem(new GUIContent("Converter"), false, () => CreateNode<ConverterNode>(worldPos));
            menu.ShowAsContext();
        }

        private void CreateNode<T>(Vector2 worldPos) where T : BalancingNode
        {
            T node = CreateInstance<T>();
            node.DisplayName = typeof(T).Name.Replace("Node", "");
            node.Position = worldPos;
            _data.Nodes.Add(node);
        }

        private void ShowNodeContextMenu(BalancingNode node)
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Delete Node"), false, () =>
            {
                string deletedNodeId = node.NodeId;
                foreach (var n in _data.Nodes)
                {
                    n.InputNodeIds.Remove(deletedNodeId);
                    n.OutputNodeIds.Remove(deletedNodeId);
                }
                _data.Nodes.Remove(node);
                _data.Connections.RemoveAll(c => c.FromNodeId == deletedNodeId || c.ToNodeId == deletedNodeId);
            });

            if (node.OutputNodeIds.Count > 0)
            {
                menu.AddItem(new GUIContent("Delete All Connections"), false, () =>
                {
                    string nodeId = node.NodeId;
                    foreach (var targetId in node.OutputNodeIds)
                    {
                        var targetNode = _data.Nodes.Find(n => n.NodeId == targetId);
                        if (targetNode != null)
                        {
                            targetNode.InputNodeIds.Remove(nodeId);
                        }
                    }
                    node.OutputNodeIds.Clear();
                    _data.Connections.RemoveAll(c => c.FromNodeId == nodeId);
                });
            }

            menu.ShowAsContext();
        }

        private void Update()
        {
            if (_inspectorWindow != null)
                Repaint();
        }

        public void OpenInspector()
        {
            if (_inspectorWindow == null)
            {
                _inspectorWindow = GetWindow<NodeInspectorWindow>("Node Inspector");
                _inspectorWindow.SetParentWindow(this);
            }
            _inspectorWindow.SetNode(_selectedNode);
            _inspectorWindow.Focus();
        }

        [MenuItem("Tools/Balancing Inspector")]
        public static void OpenInspectorWindow()
        {
            GetWindow<NodeInspectorWindow>("Node Inspector");
        }
    }
}