using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

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
        private const float SidebarWidth = 200f;
        private const float BottomPanelHeight = 180f;
        private Vector2 _currencyScrollOffset;
        private int _tickCount = 100;
        private bool _debugValues = true;
        private int _exportCurrencyIndex = 0;
        private string _exportFolderPath = "Assets";

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
                if (_data != null)
                {
                    _tickCount = _data.TickCount;
                    LoadNodesFromAsset();
                }
                return;
            }

            GUILayout.BeginHorizontal();
            BalancingData newData = (BalancingData)EditorGUILayout.ObjectField(_data, typeof(BalancingData), false);
            if (newData != _data)
            {
                _data = newData;
                if (_data != null)
                {
                    _tickCount = _data.TickCount;
                    LoadNodesFromAsset();
                }
            }
            if (GUILayout.Button("Create"))
            {
                CreateNewData();
            }
            _showGrid = GUILayout.Toggle(_showGrid, "Show Grid");
            GUILayout.Label("Zoom: " + _zoom.ToString("F1"));
            GUILayout.EndHorizontal();

            if (_showGrid)
                DrawGrid();

            DrawConnections();
            DrawNodes();

            DrawSidebar();
            DrawBottomPanel();

            ProcessInput(e);

            HandleMouseCapture();

            if (GUI.changed && _data != null)
            {
                EditorUtility.SetDirty(_data);
                AssetDatabase.SaveAssets();
            }
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
                string basePath = "Assets/BalancingData_";
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
                _tickCount = _data.TickCount;
                EditorGUIUtility.PingObject(newData);
                LoadNodesFromAsset();
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError("CreateNewData: " + ex.Message + "\n" + ex.StackTrace);
            }
        }

        private void LoadNodesFromAsset()
        {
            if (_data == null)
                return;

            if (_data.Nodes == null)
                _data.Nodes = new System.Collections.Generic.List<BalancingNode>();
            if (_data.Connections == null)
                _data.Connections = new System.Collections.Generic.List<NodeConnection>();

            UnityEngine.Object[] allSubAssets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(_data));

            var validNodes = new System.Collections.Generic.List<BalancingNode>();
            foreach (var obj in allSubAssets)
            {
                if (obj is BalancingNode node && obj != _data)
                {
                    validNodes.Add(node);
                }
            }

            if (validNodes.Count > 0)
            {
                _data.Nodes = validNodes;
                AssetDatabase.SaveAssets();
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
                            EditorUtility.SetDirty(_data);
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
                                    if (clickedNode.CanHaveOutput)
                                    {
                                        _isConnectingViaNode = true;
                                        _connectionStartNode = clickedNode;
                                        _connectionEndPoint = e.mousePosition;
                                    }
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
                            if (targetNode != null && targetNode != _connectionFromNode && targetNode.CanHaveInput)
                            {
                                CreateConnection(_connectionFromNode, targetNode);
                            }
                            _isDraggingConnection = false;
                            _connectionFromNode = null;
                        }
                        else if (_isConnectingViaNode && _connectionStartNode != null && _data != null)
                        {
                            BalancingNode targetNode = GetNodeAtMouse();
                            if (targetNode != null && targetNode != _connectionStartNode && targetNode.CanHaveInput)
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
                            if (node == null)
                                continue;
                            node.InputNodeIds.Remove(deletedNodeId);
                            node.OutputNodeIds.Remove(deletedNodeId);
                        }
                        _data.Nodes.Remove(_selectedNode);
                        AssetDatabase.RemoveObjectFromAsset(_selectedNode);
                        _data.Connections.RemoveAll(c => c.FromNodeId == deletedNodeId || c.ToNodeId == deletedNodeId);
                        AssetDatabase.SaveAssets();
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

            for (float x = gridOffset.x; x < position.width - SidebarWidth; x += scaledGridSize)
            {
                Handles.DrawLine(new Vector2(x, 0), new Vector2(x, position.height));
            }
            for (float y = gridOffset.y; y < position.height - BottomPanelHeight; y += scaledGridSize)
            {
                Handles.DrawLine(new Vector2(0, y), new Vector2(position.width - SidebarWidth, y));
            }

            Handles.EndGUI();
        }

        private void DrawSidebar()
        {
            float toolbarHeight = 35f;
            float sidebarX = position.width - SidebarWidth;
            float sidebarHeight = position.height - toolbarHeight - BottomPanelHeight;
            Rect sidebarRect = new Rect(sidebarX, toolbarHeight, SidebarWidth, sidebarHeight);
            EditorGUI.DrawRect(sidebarRect, new Color(0.15f, 0.15f, 0.15f));

            float padding = 10f;
            float sectionGap = 8f;
            float halfHeight = (sidebarHeight - padding * 2 - sectionGap) / 2;

            float y = toolbarHeight + padding;

            GUI.Label(new Rect(sidebarX + padding, y, SidebarWidth - padding * 2, 20), "Nodes");
            y += 22f;

            float nodeButtonSize = (SidebarWidth - padding * 2 - 4f) / 2;
            string[] nodeTypes = new string[] { "Source", "Drain", "Converter", "Pool" };
            for (int i = 0; i < nodeTypes.Length; i++)
            {
                float btnX = sidebarX + padding + (i % 2) * (nodeButtonSize + 4f);
                float btnY = y + (i / 2) * (nodeButtonSize + 4f);

                if (GUI.Button(new Rect(btnX, btnY, nodeButtonSize, nodeButtonSize), nodeTypes[i]))
                {
                    CreateNodeByType(nodeTypes[i]);
                }
            }

            y += nodeButtonSize * 2 + 4f;

            if (GUI.Button(new Rect(sidebarX + padding, y, SidebarWidth - padding * 2, 28), _currentMode == EditorMode.Select ? "Connection Mode" : "Select Mode"))
            {
                _currentMode = _currentMode == EditorMode.Select ? EditorMode.Connect : EditorMode.Select;
            }

            y += 32f + sectionGap;
            GUI.Label(new Rect(sidebarX + padding, y, SidebarWidth - padding * 2, 20), "Currencies");
            y += 22f;

            if (_data != null)
            {
                if (GUI.Button(new Rect(sidebarX + padding, y, 30, 20), "+"))
                {
                    _data.Currencies.Add(new CurrencyInfo { Name = "NewCurrency", Color = GetRandomCurrencyColor() });
                    _inspectorWindow?.SetData(_data);
                }
                y += 24f;

                float listHeight = sidebarHeight - (y - toolbarHeight) - padding;
                Rect scrollRect = new Rect(sidebarX + 5, y, SidebarWidth - 10, listHeight);
                _currencyScrollOffset = GUI.BeginScrollView(scrollRect, _currencyScrollOffset, new Rect(0, 0, SidebarWidth - 30, _data.Currencies.Count * 56));

                for (int i = 0; i < _data.Currencies.Count; i++)
                {
                    float itemY = i * 56;
                    _data.Currencies[i].Name = GUI.TextField(new Rect(0, itemY, SidebarWidth - 60, 20), _data.Currencies[i].Name);
                    Color newColor = EditorGUI.ColorField(new Rect(SidebarWidth - 32, itemY - 2, 30, 16), _data.Currencies[i].Color);
                    if (newColor != _data.Currencies[i].Color)
                        _data.Currencies[i].Color = newColor;
                    if (GUI.Button(new Rect(SidebarWidth - 60, itemY, 25, 20), "X"))
                    {
                        _data.Currencies.RemoveAt(i);
                        foreach (var node in _data.Nodes)
                        {
                            if (node.CurrencyIndex >= _data.Currencies.Count)
                                node.CurrencyIndex = Mathf.Max(0, _data.Currencies.Count - 1);
                        }
                        _inspectorWindow?.SetData(_data);
                        break;
                    }
                }

                GUI.EndScrollView();
            }
        }

        private Color GetRandomCurrencyColor()
        {
            Color[] presetColors = new Color[]
            {
                Color.red,
                new Color(1f, 0.5f, 0f),
                Color.yellow,
                Color.green,
                new Color(0f, 1f, 0.5f),
                Color.cyan,
                Color.blue,
                new Color(0.5f, 0f, 1f),
                new Color(1f, 0f, 0.5f),
                new Color(1f, 0.5f, 0.5f)
            };
            return presetColors[Random.Range(0, presetColors.Length)];
        }

        private void DrawBottomPanel()
        {
            float panelY = position.height - BottomPanelHeight;
            Rect panelRect = new Rect(0, panelY, position.width - SidebarWidth, BottomPanelHeight);
            EditorGUI.DrawRect(panelRect, new Color(0.12f, 0.12f, 0.12f));

            float padding = 10f;
            float controlPanelWidth = (position.width - SidebarWidth) / 4;
            float controlPanelHeight = BottomPanelHeight - padding * 2;

            Rect controlBoxRect = new Rect(padding, panelY + padding, controlPanelWidth - padding, controlPanelHeight);
            EditorGUI.DrawRect(controlBoxRect, new Color(0.2f, 0.2f, 0.2f));

            float innerPadding = 8f;
            float ctrlInnerY = panelY + padding + innerPadding;

            GUI.Label(new Rect(padding + innerPadding, ctrlInnerY, controlPanelWidth - innerPadding * 2, 16), "Ticks:");
            ctrlInnerY += 18f;

            if (_data != null)
            {
                _tickCount = EditorGUI.IntField(new Rect(padding + innerPadding, ctrlInnerY, controlPanelWidth - innerPadding * 2, 18), _tickCount);
                ctrlInnerY += 24f;

                if (GUI.Button(new Rect(padding + innerPadding, ctrlInnerY, controlPanelWidth - innerPadding * 2, 22), "Predict"))
                {
                    _data.TickCount = _tickCount;
                    _data.CalculateStatistics(_debugValues);
                }
                ctrlInnerY += 26f;

                _debugValues = GUI.Toggle(new Rect(padding + innerPadding, ctrlInnerY, controlPanelWidth - innerPadding * 2, 16), _debugValues, "Debug values");
                ctrlInnerY += 26f;

                if (_data.Currencies.Count > 0)
                {
                    string[] currencyNames = _data.Currencies.Select(c => c.Name).ToArray();
                    _exportCurrencyIndex = EditorGUI.Popup(
                        new Rect(padding + innerPadding, ctrlInnerY, controlPanelWidth - innerPadding * 2, 18),
                        _exportCurrencyIndex, currencyNames
                    );
                    ctrlInnerY += 22f;

                    _exportFolderPath = EditorGUI.TextField(
                        new Rect(padding + innerPadding, ctrlInnerY, controlPanelWidth - innerPadding * 2, 18),
                        _exportFolderPath
                    );
                    ctrlInnerY += 22f;

                    if (GUI.Button(new Rect(padding + innerPadding, ctrlInnerY, controlPanelWidth - innerPadding * 2, 22), $"Export {currencyNames[_exportCurrencyIndex]}"))
                    {
                        _data.CreateGraphSO(currencyNames[_exportCurrencyIndex], _exportFolderPath);
                    }
                    ctrlInnerY += 26f;
                }
            }

            float graphX = padding + controlPanelWidth;
            float graphWidth = position.width - SidebarWidth - controlPanelWidth - padding * 2;
            Rect graphRect = new Rect(graphX, panelY + padding, graphWidth, controlPanelHeight);
            EditorGUI.DrawRect(graphRect, new Color(0.18f, 0.18f, 0.18f));

            if (_data != null && _data.TickInfos != null && _data.TickInfos.Count > 1)
            {
                DrawGraph(graphX, graphY: panelY + padding, graphWidth, controlPanelHeight);
            }
            else
            {
                GUI.Label(new Rect(graphX + 10, panelY + BottomPanelHeight / 2 - 8, graphWidth - 20, 16), "Graph (run Predict)");
            }
        }

        private void DrawGraph(float graphX, float graphY, float graphWidth, float graphHeight)
        {
            var tickInfos = _data.TickInfos;
            int tickCount = tickInfos.Count - 1;
            if (tickCount <= 0)
                return;

            float padding = 30f;
            float labelHeight = 20f;
            float drawWidth = graphWidth - padding * 2;
            float drawHeight = graphHeight - labelHeight - padding;

            int currencyCount = _data.Currencies.Count;
            var allValues = new List<List<float>>();
            for (int c = 0; c < currencyCount; c++)
            {
                var values = new List<float>();
                foreach (var info in tickInfos)
                {
                    values.Add(info.Resources.ContainsKey(c) ? info.Resources[c] : 0f);
                }
                allValues.Add(values);
            }

            float minValue = 0f;
            float maxValue = 0f;
            for (int c = 0; c < currencyCount; c++)
            {
                float currencyMax = allValues[c].Max();
                float currencyMin = allValues[c].Min();
                if (currencyMax > maxValue)
                    maxValue = currencyMax;
                if (currencyMin < minValue && currencyMin < 0)
                    minValue = currencyMin;
            }

            if (minValue >= 0)
                minValue = 0;

            float valueRange = maxValue - minValue;
            if (valueRange < 0.001f)
                valueRange = 1f;

            Handles.BeginGUI();

            Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            Handles.DrawLine(new Vector2(graphX + padding, graphY + padding + drawHeight), new Vector2(graphX + padding + drawWidth, graphY + padding + drawHeight));
            Handles.DrawLine(new Vector2(graphX + padding, graphY + padding), new Vector2(graphX + padding, graphY + padding + drawHeight));

            for (int i = 1; i < 10; i++)
            {
                float x = graphX + padding + (float)i / 10 * drawWidth;
                Handles.DrawLine(new Vector2(x, graphY + padding), new Vector2(x, graphY + padding + drawHeight));
            }

            for (int i = 1; i < 4; i++)
            {
                float y = graphY + padding + drawHeight - (float)i / 4 * drawHeight;
                Handles.DrawLine(new Vector2(graphX + padding, y), new Vector2(graphX + padding + drawWidth, y));
            }

            for (int c = 0; c < currencyCount; c++)
            {
                var values = allValues[c];
                Color lineColor = _data.Currencies[c].Color;
                Handles.color = lineColor;

                for (int t = 0; t < tickCount; t++)
                {
                    float x1 = graphX + padding + (float)t / tickCount * drawWidth;
                    float y1 = graphY + padding + drawHeight - (values[t] - minValue) / valueRange * drawHeight;
                    float x2 = graphX + padding + (float)(t + 1) / tickCount * drawWidth;
                    float y2 = graphY + padding + drawHeight - (values[t + 1] - minValue) / valueRange * drawHeight;

                    Handles.DrawLine(new Vector2(x1, y1), new Vector2(x2, y2));
                }
            }

            Handles.EndGUI();

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 10;
            labelStyle.normal.textColor = Color.white;

            GUI.Label(new Rect(graphX + padding, graphY + graphHeight - 12, 30, 16), "0", labelStyle);
            GUI.Label(new Rect(graphX + graphWidth - padding - 20, graphY + graphHeight - 12, 30, 16), tickCount.ToString(), labelStyle);

            string maxLabel = maxValue.ToString("F0");
            GUI.Label(new Rect(graphX + 2, graphY + padding, 25, 16), maxLabel, labelStyle);

            GUIStyle tickLabelStyle = new GUIStyle(GUI.skin.label);
            tickLabelStyle.fontSize = 9;
            tickLabelStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);

            for (int i = 1; i < 10; i++)
            {
                float x = graphX + padding + (float)i / 10 * drawWidth;
                string xLabel = Mathf.RoundToInt((float)i / 10 * tickCount).ToString();
                GUI.Label(new Rect(x - 10, graphY + graphHeight - 12, 20, 14), xLabel, tickLabelStyle);
            }

            for (int i = 1; i < 4; i++)
            {
                float y = graphY + padding + drawHeight - (float)i / 4 * drawHeight;
                float yValue = minValue + (float)i / 4 * valueRange;
                string yLabel = yValue.ToString("F0");
                GUI.Label(new Rect(graphX + 2, y - 6, 25, 14), yLabel, tickLabelStyle);
            }
        }

        private void CreateNodeByType(string nodeType)
        {
            if (_data == null)
                return;

            float centerX = (position.width - SidebarWidth) / 2;
            float centerY = (position.height - BottomPanelHeight) / 2;
            Vector2 spawnPos = new Vector2(centerX, centerY);
            spawnPos = (spawnPos - _scrollOffset) / _zoom;

            switch (nodeType)
            {
                case "Source":
                    CreateNode<SourceNode>(spawnPos);
                    break;
                case "Drain":
                    CreateNode<DrainNode>(spawnPos);
                    break;
                case "Pool":
                    CreateNode<PoolNode>(spawnPos);
                    break;
                case "Converter":
                    CreateNode<ConverterNode>(spawnPos);
                    break;
            }
        }

        private void DrawNodes()
        {
            if (_data == null || _data.Nodes == null)
            {
                _selectedNode = null;
                _inspectorWindow?.SetNode(null);
            }

            foreach (var node in _data.Nodes)
            {
                if (node == null)
                    continue;

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

                if (node.CanHaveInput)
                    Handles.DrawSolidDisc(inputDotPos, Vector3.forward, ConnectionPointRadius * _zoom);
                if (node.CanHaveOutput)
                {
                    Handles.color = nodeColor;
                    Handles.DrawSolidDisc(outputDotPos, Vector3.forward, ConnectionPointRadius * _zoom);
                }
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
            if (!fromNode.CanHaveOutput || !toNode.CanHaveInput)
                return;

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
            if (_data == null || _data.Connections == null || _data.Nodes == null)
                return;

            Handles.BeginGUI();

            foreach (var conn in _data.Connections)
            {
                if (string.IsNullOrEmpty(conn.FromNodeId) || string.IsNullOrEmpty(conn.ToNodeId))
                    continue;

                BalancingNode fromNode = _data.Nodes.Find(n => n != null && n.NodeId == conn.FromNodeId);
                BalancingNode toNode = _data.Nodes.Find(n => n != null && n.NodeId == conn.ToNodeId);

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
                if (node == null)
                    continue;
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
                if (n == null)
                    continue;
                if (!n.CanHaveOutput)
                    continue;

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
            if (_data == null || _data.Connections == null || _data.Nodes == null)
                return null;

            Vector2 mousePos = (Event.current.mousePosition + _scrollOffset) / _zoom;

            foreach (var conn in _data.Connections)
            {
                BalancingNode fromNode = _data.Nodes.Find(n => n != null && n.NodeId == conn.FromNodeId);
                BalancingNode toNode = _data.Nodes.Find(n => n != null && n.NodeId == conn.ToNodeId);

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
            node.hideFlags = HideFlags.None;
            node.DisplayName = typeof(T).Name.Replace("Node", "");
            node.Position = worldPos;
            _data.Nodes.Add(node);
            AssetDatabase.AddObjectToAsset(node, _data);
            AssetDatabase.SaveAssets();
        }

        private void ShowNodeContextMenu(BalancingNode node)
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Delete Node"), false, () =>
            {
                string deletedNodeId = node.NodeId;
                foreach (var n in _data.Nodes)
                {
                    if (n == null)
                        continue;
                    n.InputNodeIds.Remove(deletedNodeId);
                    n.OutputNodeIds.Remove(deletedNodeId);
                }
                _data.Nodes.Remove(node);
                AssetDatabase.RemoveObjectFromAsset(node);
                _data.Connections.RemoveAll(c => c.FromNodeId == deletedNodeId || c.ToNodeId == deletedNodeId);
                AssetDatabase.SaveAssets();
            });

            if (node.OutputNodeIds.Count > 0)
            {
                menu.AddItem(new GUIContent("Delete All Connections"), false, () =>
                {
                    string nodeId = node.NodeId;
                    foreach (var targetId in node.OutputNodeIds)
                    {
                        var targetNode = _data.Nodes.Find(n => n != null && n.NodeId == targetId);
                        if (targetNode != null)
                        {
                            targetNode.InputNodeIds.Remove(nodeId);
                        }
                    }
                    node.OutputNodeIds.Clear();
                    _data.Connections.RemoveAll(c => c.FromNodeId == nodeId);
                    AssetDatabase.SaveAssets();
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
            _inspectorWindow.SetData(_data);
            _inspectorWindow.SetNode(_selectedNode);
            _inspectorWindow.SetCurrentTick(_tickCount);
            _inspectorWindow.Focus();
        }

        public static void OpenInspectorWindow()
        {
            GetWindow<NodeInspectorWindow>("Node Inspector");
        }
    }
}