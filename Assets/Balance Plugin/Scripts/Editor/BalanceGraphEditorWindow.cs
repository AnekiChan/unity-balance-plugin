using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace BalancePlugin
{
    public class BalanceGraphEditorWindow : EditorWindow
    {
        private BalanceGraph _graph;
        private BalanceNode _selectedNode;
        private BalanceNode _connectionStartNode;
        private bool _isDraggingNode;
        private bool _isPanning;
        private bool _isConnecting;
        private Vector2 _dragOffset;
        private Vector2 _panOffset;
        private Vector2 _mousePosition;
        private Vector2 _dragStartPosition;
        private float _dragDistance;
        private float _zoom = 1f;

        private const float NodeWidth = 120f;
        private const float NodeHeight = 60f;
        private const float ConnectionArrowSize = 10f;

        private float _gridSize = 20f;
        private bool _showGrid = true;

        private static readonly Dictionary<NodeType, Color> NodeColors = new Dictionary<NodeType, Color>
        {
            { NodeType.Source, new Color(0.2f, 0.8f, 0.2f) },
            { NodeType.Drain, new Color(0.8f, 0.2f, 0.2f) },
            { NodeType.Converter, new Color(0.2f, 0.4f, 0.8f) },
            { NodeType.Pool, new Color(0.8f, 0.6f, 0.2f) },
            { NodeType.Gate, new Color(0.6f, 0.2f, 0.6f) }
        };

        [MenuItem("Window/Balance Plugin/Balance Graph")]
        public static void ShowWindow()
        {
            var window = GetWindow<BalanceGraphEditorWindow>("Balance Graph");
            window.minSize = new Vector2(400, 300);
        }

        public static void ShowWindow(BalanceGraph graph)
        {
            var window = GetWindow<BalanceGraphEditorWindow>("Balance Graph");
            window.LoadGraph(graph);
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawSettingsBar();

            var areaRect = GUILayoutUtility.GetRect(0, 0, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            _mousePosition = Event.current.mousePosition - areaRect.position;

            if (_graph != null)
            {
                _mousePosition = ScreenToWorld(_mousePosition);

                if (_showGrid)
                {
                    DrawGrid(areaRect);
                }
                DrawConnections();
                DrawTempConnection();
                DrawNodes();

                ProcessEvents(areaRect);
            }
            else
            {
                DrawEmptyState();
            }

            if (GUI.changed && _graph != null)
            {
                EditorUtility.SetDirty(_graph);
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            EditorGUILayout.LabelField("Graph:", GUILayout.Width(50));

            var newGraph = (BalanceGraph)EditorGUILayout.ObjectField(_graph, typeof(BalanceGraph), false);
            if (newGraph != _graph)
            {
                LoadGraph(newGraph);
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("New Graph", EditorStyles.toolbarButton))
            {
                CreateNewGraph();
            }

            if (GUILayout.Button("Clear", EditorStyles.toolbarButton))
            {
                if (_graph != null && EditorUtility.DisplayDialog("Clear Graph", "Are you sure you want to clear all nodes?", "Clear", "Cancel"))
                {
                    _graph.Nodes.Clear();
                    _graph.Connections.Clear();
                    _selectedNode = null;
                    _connectionStartNode = null;
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSettingsBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            _showGrid = GUILayout.Toggle(_showGrid, "Grid", EditorStyles.toolbarButton, GUILayout.Width(50));
            _gridSize = EditorGUILayout.FloatField("Grid Size", _gridSize, EditorStyles.toolbarTextField, GUILayout.Width(100));
            _gridSize = Mathf.Max(5f, _gridSize);

            GUILayout.FlexibleSpace();

            EditorGUILayout.LabelField($"Zoom: {_zoom:F1}x", GUILayout.Width(80));

            EditorGUILayout.EndHorizontal();
        }

        private void DrawEmptyState()
        {
            GUILayout.BeginArea(new Rect(0, 60, position.width, position.height - 60));
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            GUILayout.Label("No graph loaded", EditorStyles.boldLabel);
            GUILayout.Label("Create a new graph or assign an existing one");
            GUILayout.Space(10);

            if (GUILayout.Button("Create New Balance Graph", GUILayout.Height(30)))
            {
                CreateNewGraph();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void CreateNewGraph()
        {
            var path = EditorUtility.SaveFilePanelInProject("Create Balance Graph", "NewBalanceGraph", "asset", "Save Balance Graph");
            if (!string.IsNullOrEmpty(path))
            {
                var graph = ScriptableObject.CreateInstance<BalanceGraph>();
                AssetDatabase.CreateAsset(graph, path);
                AssetDatabase.SaveAssets();
                LoadGraph(graph);
            }
        }

        public void LoadGraph(BalanceGraph graph)
        {
            _graph = graph;
            _selectedNode = null;
            _connectionStartNode = null;
            _isDraggingNode = false;
            _isConnecting = false;
            _panOffset = Vector2.zero;
            _zoom = 1f;
        }

        private Vector2 ScreenToWorld(Vector2 screenPos)
        {
            return (screenPos - _panOffset) / _zoom;
        }

        private Vector2 WorldToScreen(Vector2 worldPos)
        {
            return worldPos * _zoom + _panOffset;
        }

        private void DrawGrid(Rect areaRect)
        {
            var worldStart = ScreenToWorld(Vector2.zero);
            var worldEnd = ScreenToWorld(new Vector2(areaRect.width, areaRect.height));

            var gridColor = new Color(0.3f, 0.3f, 0.3f, 0.4f);
            var majorGridColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);

            Handles.BeginGUI();
            Handles.color = gridColor;

            var startX = Mathf.Floor(worldStart.x / _gridSize) * _gridSize;
            var startY = Mathf.Floor(worldStart.y / _gridSize) * _gridSize;

            for (float x = startX; x < worldEnd.x + _gridSize; x += _gridSize)
            {
                var isMajor = Mathf.Abs(x % (_gridSize * 5)) < 0.01f;
                Handles.color = isMajor ? majorGridColor : gridColor;
                var screenX = WorldToScreen(new Vector2(x, 0)).x;
                Handles.DrawLine(new Vector3(screenX, 0, 0), new Vector3(screenX, areaRect.height, 0));
            }

            for (float y = startY; y < worldEnd.y + _gridSize; y += _gridSize)
            {
                var isMajor = Mathf.Abs(y % (_gridSize * 5)) < 0.01f;
                Handles.color = isMajor ? majorGridColor : gridColor;
                var screenY = WorldToScreen(new Vector2(0, y)).y;
                Handles.DrawLine(new Vector3(0, screenY, 0), new Vector3(areaRect.width, screenY, 0));
            }

            Handles.EndGUI();
        }

        private void DrawNodes()
        {
            if (_graph == null) return;

            foreach (var node in _graph.Nodes)
            {
                DrawNodeGUI(node);
            }
        }

        private void DrawNodeGUI(BalanceNode node)
        {
            var screenPos = WorldToScreen(node.Position);
            var screenWidth = NodeWidth * _zoom;
            var screenHeight = NodeHeight * _zoom;

            var rect = new Rect(screenPos.x, screenPos.y, screenWidth, screenHeight);
            var color = NodeColors[node.Type];

            var borderColor = _selectedNode == node ? Color.white : color;
            EditorGUI.DrawRect(new Rect(rect.x - 2, rect.y - 2, rect.width + 4, rect.height + 4), borderColor);
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f, 1f));

            var labelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(11 * _zoom),
                normal = { textColor = Color.white }
            };
            EditorGUI.DropShadowLabel(rect, node.Label, labelStyle);

            var typeStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(9 * _zoom),
                normal = { textColor = Color.gray }
            };
            GUI.Label(new Rect(rect.x, rect.y + rect.height - 14 * _zoom, rect.width, 14 * _zoom), node.Type.ToString(), typeStyle);

            var valueStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(9 * _zoom),
                normal = { textColor = Color.yellow }
            };
            GUI.Label(new Rect(rect.x, rect.y - 14 * _zoom, rect.width, 14 * _zoom), $"Value: {node.Value:F1}", valueStyle);

            if (_isConnecting && _connectionStartNode == node)
            {
                EditorGUI.DrawRect(new Rect(rect.x - 4, rect.y - 4, rect.width + 8, rect.height + 8), new Color(0, 1, 1, 0.5f));
            }
        }

        private void DrawConnections()
        {
            if (_graph == null) return;

            Handles.BeginGUI();
            foreach (var connection in _graph.Connections)
            {
                var fromNode = _graph.GetNode(connection.FromNodeId);
                var toNode = _graph.GetNode(connection.ToNodeId);

                if (fromNode != null && toNode != null)
                {
                    DrawConnection(connection, fromNode, toNode);
                }
            }
            Handles.EndGUI();
        }

        private void DrawTempConnection()
        {
            if (_graph == null || _connectionStartNode == null) return;

            var start = WorldToScreen(_connectionStartNode.Position + new Vector2(NodeWidth, NodeHeight / 2));
            var end = Event.current.mousePosition;

            Handles.BeginGUI();
            Handles.color = new Color(0, 1, 1, 1f);
            Handles.DrawLine(start, end);
            Handles.EndGUI();
        }

        private void DrawConnection(NodeConnection connection, BalanceNode from, BalanceNode to)
        {
            var fromScreen = WorldToScreen(from.Position + new Vector2(NodeWidth, NodeHeight / 2));
            var toScreen = WorldToScreen(to.Position + new Vector2(0, NodeHeight / 2));

            Handles.color = Color.cyan;
            Handles.DrawLine(fromScreen, toScreen);

            var dir = (toScreen - fromScreen).normalized;
            var arrowSize = ConnectionArrowSize * _zoom;
            var arrowLeft = toScreen - dir * arrowSize + new Vector2(-dir.y, dir.x) * arrowSize * 0.5f;
            var arrowRight = toScreen - dir * arrowSize - new Vector2(-dir.y, dir.x) * arrowSize * 0.5f;
            Handles.DrawLine(toScreen, arrowLeft);
            Handles.DrawLine(toScreen, arrowRight);

            var midPoint = (fromScreen + toScreen) * 0.5f;
            var labelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(9 * _zoom),
                normal = { textColor = Color.cyan }
            };
            GUI.Label(new Rect(midPoint.x - 30, midPoint.y - 10, 60, 20), $"Flow: {connection.FlowRate:F1}", labelStyle);
        }

        private NodeConnection GetConnectionAtPoint(Vector2 worldPoint)
        {
            foreach (var connection in _graph.Connections)
            {
                var fromNode = _graph.GetNode(connection.FromNodeId);
                var toNode = _graph.GetNode(connection.ToNodeId);

                if (fromNode == null || toNode == null) continue;

                var fromScreen = WorldToScreen(fromNode.Position + new Vector2(NodeWidth, NodeHeight / 2));
                var toScreen = WorldToScreen(toNode.Position + new Vector2(0, NodeHeight / 2));

                var screenPoint = WorldToScreen(worldPoint);
                var distance = DistanceToLineSegment(screenPoint, fromScreen, toScreen);
                if (distance < 10f * _zoom)
                {
                    return connection;
                }
            }
            return null;
        }

        private float DistanceToLineSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            var ap = p - a;
            var t = Vector2.Dot(ap, ab) / Vector2.Dot(ab, ab);
            t = Mathf.Clamp01(t);
            var closest = a + ab * t;
            return Vector2.Distance(p, closest);
        }

        private BalanceNode GetNodeAtPoint(Vector2 worldPoint)
        {
            foreach (var node in _graph.Nodes)
            {
                var screenRect = new Rect(
                    WorldToScreen(node.Position).x,
                    WorldToScreen(node.Position).y,
                    NodeWidth * _zoom,
                    NodeHeight * _zoom
                );
                if (screenRect.Contains(Event.current.mousePosition))
                {
                    return node;
                }
            }
            return null;
        }

        private void ProcessEvents(Rect areaRect)
        {
            var currentEvent = Event.current;
            var worldMousePos = ScreenToWorld(currentEvent.mousePosition - areaRect.position);

            switch (currentEvent.type)
            {
                case EventType.MouseDown:
                    if (currentEvent.button == 1)
                    {
                        ShowContextMenu();
                        currentEvent.Use();
                    }
                    else if (currentEvent.button == 2)
                    {
                        _isPanning = true;
                        _dragOffset = currentEvent.mousePosition;
                        currentEvent.Use();
                    }
                    else if (currentEvent.button == 0)
                    {
                        if (_isConnecting && _connectionStartNode != null)
                        {
                            var targetNode = GetNodeAtPoint(worldMousePos);
                            if (targetNode != null && targetNode != _connectionStartNode)
                            {
                                _graph.AddConnection(_connectionStartNode.Id, targetNode.Id);
                                GUI.changed = true;
                            }
                            _connectionStartNode = null;
                            _isConnecting = false;
                            currentEvent.Use();
                        }
                        else
                        {
                            var node = GetNodeAtPoint(worldMousePos);
                            if (node != null)
                            {
                                _selectedNode = node;
                                _isDraggingNode = true;
                                _dragOffset = currentEvent.mousePosition - WorldToScreen(node.Position);
                                _dragStartPosition = currentEvent.mousePosition;
                                _dragDistance = 0f;
                            }
                            else
                            {
                                _selectedNode = null;
                            }
                        }
                    }
                    break;

                case EventType.MouseDrag:
                    if (_isPanning)
                    {
                        _panOffset += currentEvent.delta;
                        currentEvent.Use();
                    }
                    else if (_isDraggingNode && _selectedNode != null)
                    {
                        _dragDistance += currentEvent.delta.magnitude;
                        var newScreenPos = currentEvent.mousePosition - _dragOffset;
                        _selectedNode.Position = ScreenToWorld(newScreenPos);
                        GUI.changed = true;
                        currentEvent.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (_isDraggingNode)
                    {
                        var wasDragging = _dragDistance > 5f;

                        if (!wasDragging && _selectedNode != null)
                        {
                            ShowNodeEditor(_selectedNode);
                        }

                        _isDraggingNode = false;
                    }

                    if (_isPanning)
                    {
                        _isPanning = false;
                    }
                    break;

                case EventType.ScrollWheel:
                    {
                        var zoomDelta = currentEvent.delta.y * 0.02f;
                        var oldZoom = _zoom;
                        _zoom = Mathf.Clamp(_zoom - zoomDelta, 0.25f, 3f);

                        _panOffset -= (currentEvent.mousePosition - areaRect.position) * (_zoom - oldZoom);

                        currentEvent.Use();
                    }
                    break;

                case EventType.KeyDown:
                    if (currentEvent.keyCode == KeyCode.Escape && _isConnecting)
                    {
                        _isConnecting = false;
                        _connectionStartNode = null;
                        currentEvent.Use();
                    }
                    else if (currentEvent.keyCode == KeyCode.Delete && _selectedNode != null)
                    {
                        _graph.RemoveNode(_selectedNode.Id);
                        _selectedNode = null;
                        GUI.changed = true;
                        currentEvent.Use();
                    }
                    else if (currentEvent.keyCode == KeyCode.F)
                    {
                        _panOffset = Vector2.zero;
                        _zoom = 1f;
                        currentEvent.Use();
                    }
                    break;
            }
        }

        private void ShowContextMenu()
        {
            var menu = new GenericMenu();
            var worldMousePos = ScreenToWorld(_mousePosition);

            var nodeAtPoint = GetNodeAtPoint(worldMousePos);
            var connectionAtPoint = GetConnectionAtPoint(worldMousePos);

            if (connectionAtPoint != null)
            {
                menu.AddItem(new GUIContent("Edit Connection"), false, () => ShowConnectionEditor(connectionAtPoint));
                menu.AddItem(new GUIContent("Delete Connection"), false, () => DeleteConnection(connectionAtPoint));
            }
            else if (nodeAtPoint != null)
            {
                menu.AddItem(new GUIContent("Edit Node"), false, () => ShowNodeEditor(nodeAtPoint));
                menu.AddItem(new GUIContent("Delete Node"), false, () => DeleteNode(nodeAtPoint));
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Connect From Here"), false, () => StartConnection(nodeAtPoint));
            }
            else
            {
                menu.AddItem(new GUIContent("Add Node/Source"), false, () => AddNode(NodeType.Source, worldMousePos));
                menu.AddItem(new GUIContent("Add Node/Drain"), false, () => AddNode(NodeType.Drain, worldMousePos));
                menu.AddItem(new GUIContent("Add Node/Converter"), false, () => AddNode(NodeType.Converter, worldMousePos));
                menu.AddItem(new GUIContent("Add Node/Pool"), false, () => AddNode(NodeType.Pool, worldMousePos));
                menu.AddItem(new GUIContent("Add Node/Gate"), false, () => AddNode(NodeType.Gate, worldMousePos));
            }

            menu.ShowAsContext();
        }

        private void AddNode(NodeType type, Vector2 position)
        {
            if (_graph == null)
            {
                CreateNewGraph();
            }

            var node = _graph.AddNode(position);
            node.Type = type;
            node.Label = type.ToString();
            _selectedNode = node;
            GUI.changed = true;
        }

        private void DeleteNode(BalanceNode node)
        {
            if (_selectedNode == node)
            {
                _selectedNode = null;
            }
            _graph.RemoveNode(node.Id);
            GUI.changed = true;
        }

        private void StartConnection(BalanceNode node)
        {
            _connectionStartNode = node;
            _isConnecting = true;
        }

        private void ShowNodeEditor(BalanceNode node)
        {
            NodeEditorWindow.ShowWindow(node, _graph);
        }

        private void ShowConnectionEditor(NodeConnection connection)
        {
            ConnectionEditorWindow.ShowWindow(connection, _graph);
        }

        private void DeleteConnection(NodeConnection connection)
        {
            _graph.RemoveConnection(connection.FromNodeId, connection.ToNodeId);
            GUI.changed = true;
        }
    }
}
