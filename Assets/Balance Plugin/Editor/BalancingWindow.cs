using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace BalancePlugin
{
    public class BalancingWindow : EditorWindow
    {
        private const float MinRightPanelWidth = 180f;
        private const float MaxRightPanelWidth = 380f;
        private const float MinBottomPanelHeight = 120f;
        private const float MaxBottomPanelHeight = 360f;

        private BalancingData _data;
        private BalanceGraphView _graphView;
        private VisualElement _rightPanel;
        private IMGUIContainer _bottomPanel;
        private ScrollView _currencyList;
        private Label _emptyState;
        private float _rightPanelWidth = 240f;
        private float _bottomPanelHeight = 210f;
        private int _tickCount = 100;
        private bool _debugValues = true;
        private int _exportCurrencyIndex;
        private string _exportFolderPath = "Assets";

        [MenuItem("Tools/Balance/Balance Window")]
        public static void ShowWindow()
        {
            GetWindow<BalancingWindow>("Balance");
        }

        private void OnEnable()
        {
            BuildWindow();
        }

        private void OnDisable()
        {
            _graphView?.SaveNodePositions();
        }

        private void BuildWindow()
        {
            rootVisualElement.Clear();
            rootVisualElement.style.flexDirection = FlexDirection.Column;

            var toolbar = new Toolbar();
            toolbar.Add(new Label("Data") { style = { marginLeft = 6, unityFontStyleAndWeight = FontStyle.Bold } });

            var dataField = new ObjectField
            {
                objectType = typeof(BalancingData),
                allowSceneObjects = false,
                value = _data,
                style = { minWidth = 260, flexGrow = 1 }
            };
            dataField.RegisterValueChangedCallback(evt => SetData(evt.newValue as BalancingData));
            toolbar.Add(dataField);

            toolbar.Add(new ToolbarButton(CreateNewDataInAssets) { text = "Create" });
            toolbar.Add(new ToolbarButton(() => _graphView?.FrameAll()) { text = "Frame" });
            toolbar.Add(new ToolbarButton(() => Selection.activeObject = _data) { text = "Select Data" });
            rootVisualElement.Add(toolbar);

            var content = new VisualElement { style = { flexDirection = FlexDirection.Row, flexGrow = 1 } };
            rootVisualElement.Add(content);

            _graphView = new BalanceGraphView(this)
            {
                style = { flexGrow = 1 }
            };
            _graphView.NodeSelected += SelectNodeInInspector;
            _graphView.GraphChanged += RefreshPanels;
            content.Add(_graphView);

            var rightSplitter = CreateVerticalSplitter();
            content.Add(rightSplitter);

            _rightPanel = new VisualElement
            {
                style =
                {
                    width = _rightPanelWidth,
                    backgroundColor = new Color(0.16f, 0.16f, 0.16f),
                    borderLeftColor = new Color(0.25f, 0.25f, 0.25f),
                    borderLeftWidth = 1,
                    paddingLeft = 10,
                    paddingRight = 10,
                    paddingTop = 10,
                    paddingBottom = 10
                }
            };
            content.Add(_rightPanel);

            var bottomSplitter = CreateHorizontalSplitter();
            rootVisualElement.Add(bottomSplitter);

            _bottomPanel = new IMGUIContainer(DrawBottomPanel)
            {
                style =
                {
                    height = _bottomPanelHeight,
                    backgroundColor = new Color(0.12f, 0.12f, 0.12f),
                    borderTopColor = new Color(0.25f, 0.25f, 0.25f),
                    borderTopWidth = 1
                }
            };
            rootVisualElement.Add(_bottomPanel);

            _emptyState = new Label("Select or create Balance Data")
            {
                style =
                {
                    position = Position.Absolute,
                    left = 18,
                    top = 48,
                    fontSize = 16,
                    color = new Color(0.75f, 0.75f, 0.75f)
                }
            };
            _graphView.Add(_emptyState);

            RebuildRightPanel();
            SetData(_data);
        }

        private VisualElement CreateVerticalSplitter()
        {
            var splitter = new VisualElement
            {
                style =
                {
                    width = 5,
                    backgroundColor = new Color(0.1f, 0.1f, 0.1f)
                }
            };

            bool dragging = false;
            splitter.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button != 0)
                    return;
                dragging = true;
                splitter.CaptureMouse();
                evt.StopPropagation();
            });
            splitter.RegisterCallback<MouseMoveEvent>(evt =>
            {
                if (!dragging)
                    return;
                _rightPanelWidth = Mathf.Clamp(_rightPanelWidth - evt.mouseDelta.x, MinRightPanelWidth, MaxRightPanelWidth);
                _rightPanel.style.width = _rightPanelWidth;
                evt.StopPropagation();
            });
            splitter.RegisterCallback<MouseUpEvent>(evt =>
            {
                dragging = false;
                splitter.ReleaseMouse();
                evt.StopPropagation();
            });
            return splitter;
        }

        private VisualElement CreateHorizontalSplitter()
        {
            var splitter = new VisualElement
            {
                style =
                {
                    height = 5,
                    backgroundColor = new Color(0.1f, 0.1f, 0.1f)
                }
            };

            bool dragging = false;
            splitter.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button != 0)
                    return;
                dragging = true;
                splitter.CaptureMouse();
                evt.StopPropagation();
            });
            splitter.RegisterCallback<MouseMoveEvent>(evt =>
            {
                if (!dragging)
                    return;
                _bottomPanelHeight = Mathf.Clamp(_bottomPanelHeight - evt.mouseDelta.y, MinBottomPanelHeight, MaxBottomPanelHeight);
                _bottomPanel.style.height = _bottomPanelHeight;
                evt.StopPropagation();
            });
            splitter.RegisterCallback<MouseUpEvent>(evt =>
            {
                dragging = false;
                splitter.ReleaseMouse();
                evt.StopPropagation();
            });
            return splitter;
        }

        private void SetData(BalancingData data)
        {
            _graphView?.SaveNodePositions();
            _data = data;

            if (_data != null)
            {
                _tickCount = Mathf.Max(1, _data.TickCount);
                LoadNodesFromAsset();
            }

            _graphView?.Populate(_data);
            RefreshPanels();
        }

        private void LoadNodesFromAsset()
        {
            if (_data == null)
                return;

            _data.Nodes ??= new List<BalancingNode>();
            _data.Arrows ??= new List<Arrow>();

            string path = AssetDatabase.GetAssetPath(_data);
            if (string.IsNullOrEmpty(path))
                return;

            var subAssetNodes = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<BalancingNode>()
                .Where(node => node != null)
                .ToList();

            if (subAssetNodes.Count > 0)
                _data.Nodes = subAssetNodes;

            var subAssetArrows = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Arrow>()
                .Where(arrow => arrow != null)
                .ToList();

            if (subAssetArrows.Count > 0)
                _data.Arrows = subAssetArrows;

            PruneInvalidConnections();
            SyncNodeConnectionLists();
            EditorUtility.SetDirty(_data);
        }

        private void SyncNodeConnectionLists()
        {
            foreach (BalancingNode node in _data.Nodes)
            {
                if (node == null) continue;
                node.InputNodeIds.Clear();
                node.OutputNodeIds.Clear();
            }

            foreach (Arrow arrow in _data.Arrows)
            {
                if (arrow == null) continue;
                BalancingNode from = _data.GetNode(arrow.FromNodeId);
                BalancingNode to = _data.GetNode(arrow.ToNodeId);
                if (from != null) from.OutputNodeIds.Add(arrow.ToNodeId);
                if (to != null) to.InputNodeIds.Add(arrow.FromNodeId);
            }
        }

        private void PruneInvalidConnections()
        {
            if (_data == null)
                return;

            var nodeIds = new HashSet<string>(_data.Nodes.Where(n => n != null).Select(n => n.NodeId));
            _data.Arrows.RemoveAll(a => a == null || !nodeIds.Contains(a.FromNodeId) || !nodeIds.Contains(a.ToNodeId));
        }

        private void RebuildRightPanel()
        {
            _rightPanel.Clear();

            AddPanelTitle(_rightPanel, "Nodes");
            AddNodeButton("Source", () => CreateNodeAtGraphCenter<SourceNode>());
            AddNodeButton("Drain", () => CreateNodeAtGraphCenter<DrainNode>());
            AddNodeButton("Converter", () => CreateNodeAtGraphCenter<ConverterNode>());
            AddNodeButton("Pool", () => CreateNodeAtGraphCenter<PoolNode>());

            var spacer = new VisualElement { style = { height = 12 } };
            _rightPanel.Add(spacer);

            AddPanelTitle(_rightPanel, "Currencies");
            var addCurrency = new Button(AddCurrency) { text = "+ Add Currency" };
            addCurrency.style.marginBottom = 8;
            _rightPanel.Add(addCurrency);

            _currencyList = new ScrollView { style = { flexGrow = 1 } };
            _rightPanel.Add(_currencyList);
            RefreshCurrencyList();
        }

        private void AddPanelTitle(VisualElement parent, string text)
        {
            parent.Add(new Label(text)
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 13,
                    marginBottom = 6,
                    color = new Color(0.88f, 0.88f, 0.88f)
                }
            });
        }

        private void AddNodeButton(string label, System.Action onClick)
        {
            var button = new Button(onClick) { text = label };
            button.style.height = 28;
            button.style.marginBottom = 4;
            _rightPanel.Add(button);
        }

        private void RefreshPanels()
        {
            if (_emptyState != null)
                _emptyState.style.display = _data == null ? DisplayStyle.Flex : DisplayStyle.None;
            RefreshCurrencyList();
            _bottomPanel?.MarkDirtyRepaint();
        }

        private void RefreshCurrencyList()
        {
            if (_currencyList == null)
                return;

            _currencyList.Clear();
            if (_data == null)
            {
                _currencyList.Add(new Label("No Balance Data selected."));
                return;
            }

            for (int i = 0; i < _data.Currencies.Count; i++)
            {
                int index = i;
                CurrencyInfo currency = _data.Currencies[index];
                var row = new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                        alignItems = Align.Center,
                        marginBottom = 5
                    }
                };

                var color = new ColorField { value = currency.Color, style = { width = 44 } };
                color.RegisterValueChangedCallback(evt =>
                {
                    currency.Color = evt.newValue;
                    MarkDataDirty();
                });
                row.Add(color);

                var toggle = new Toggle { value = currency.Visible, style = { width = 18 } };
                toggle.RegisterValueChangedCallback(evt =>
                {
                    currency.Visible = evt.newValue;
                    MarkDataDirty();
                    _bottomPanel?.MarkDirtyRepaint();
                });
                row.Add(toggle);

                var name = new TextField { value = currency.Name, style = { flexGrow = 1, marginLeft = 4 } };
                name.RegisterValueChangedCallback(evt =>
                {
                    currency.Name = string.IsNullOrWhiteSpace(evt.newValue) ? "Currency" : evt.newValue;
                    MarkDataDirty();
                    _bottomPanel?.MarkDirtyRepaint();
                });
                row.Add(name);

                var remove = new Button(() => RemoveCurrency(index)) { text = "X", style = { width = 28, marginLeft = 4 } };
                row.Add(remove);
                _currencyList.Add(row);
            }
        }

        private void AddCurrency()
        {
            if (_data == null)
                return;
            _data.Currencies.Add(new CurrencyInfo { Name = "NewCurrency", Color = GetRandomCurrencyColor() });
            MarkDataDirty();
            RefreshPanels();
        }

        private void RemoveCurrency(int index)
        {
            if (_data == null || index < 0 || index >= _data.Currencies.Count)
                return;

            _data.Currencies.RemoveAt(index);
            int maxIndex = Mathf.Max(0, _data.Currencies.Count - 1);
            foreach (BalancingNode node in _data.Nodes)
            {
                if (node != null && node.CurrencyIndex > maxIndex)
                    node.CurrencyIndex = maxIndex;
            }
            foreach (Arrow arrow in _data.Arrows)
            {
                if (arrow != null && arrow.CurrencyIndex > maxIndex)
                    arrow.CurrencyIndex = maxIndex;
            }
            MarkDataDirty();
            RefreshPanels();
        }

        private Color GetRandomCurrencyColor()
        {
            Color[] presetColors =
            {
                new Color(0.95f, 0.25f, 0.2f),
                new Color(1f, 0.58f, 0.16f),
                new Color(0.95f, 0.78f, 0.18f),
                new Color(0.23f, 0.73f, 0.34f),
                new Color(0.1f, 0.72f, 0.85f),
                new Color(0.28f, 0.45f, 1f),
                new Color(0.72f, 0.35f, 0.95f)
            };
            return presetColors[Random.Range(0, presetColors.Length)];
        }

        private void DrawBottomPanel()
        {
            if (_data == null)
            {
                EditorGUILayout.HelpBox("Select or create Balance Data to run simulation and export graphs.", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical(GUILayout.Width(280));
            EditorGUILayout.LabelField("Simulation", EditorStyles.boldLabel);
            _tickCount = Mathf.Max(1, EditorGUILayout.IntField("Ticks", _tickCount));
            _debugValues = EditorGUILayout.Toggle("Debug Values", _debugValues);

            if (GUILayout.Button("Predict", GUILayout.Height(26)))
            {
                _data.TickCount = _tickCount;
                _data.CalculateStatistics(_debugValues);
                MarkDataDirty();
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Export", EditorStyles.boldLabel);

            if (_data.Currencies.Count > 0)
            {
                _exportCurrencyIndex = Mathf.Clamp(_exportCurrencyIndex, 0, _data.Currencies.Count - 1);
                _exportCurrencyIndex = EditorGUILayout.Popup("Currency", _exportCurrencyIndex, _data.Currencies.Select(c => c.Name).ToArray());
                _exportFolderPath = EditorGUILayout.TextField("Folder", _exportFolderPath);

                if (GUILayout.Button("Export Graph", GUILayout.Height(24)))
                {
                    _data.CreateGraphSO(_data.Currencies[_exportCurrencyIndex].Name, _exportFolderPath);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Add at least one currency before exporting.", MessageType.None);
            }

            EditorGUILayout.EndVertical();

            Rect graphRect = GUILayoutUtility.GetRect(10, 10000, 100, 10000, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            DrawGraphPanel(graphRect);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawGraphPanel(Rect rect)
        {
            EditorGUI.DrawRect(rect, new Color(0.17f, 0.17f, 0.17f));

            if (_data.TickInfos == null || _data.TickInfos.Count <= 1)
            {
                GUI.Label(rect, "Graph (run Predict)", CenteredLabelStyle());
                return;
            }

            DrawGraph(rect);
        }

        private GUIStyle CenteredLabelStyle()
        {
            return new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.72f, 0.72f, 0.72f) }
            };
        }

        private void DrawGraph(Rect rect)
        {
            List<TickInfo> tickInfos = _data.TickInfos;
            int tickCount = tickInfos.Count - 1;
            if (tickCount <= 0)
                return;

            float padding = 34f;
            float drawWidth = Mathf.Max(1, rect.width - padding * 2);
            float drawHeight = Mathf.Max(1, rect.height - padding * 1.6f);

            int currencyCount = _data.Currencies.Count;
            var visibleIndices = new List<int>();
            for (int c = 0; c < currencyCount; c++)
            {
                if (_data.Currencies[c].Visible)
                    visibleIndices.Add(c);
            }

            var allValues = new List<List<float>>();
            for (int c = 0; c < currencyCount; c++)
                allValues.Add(tickInfos.Select(info => info.Resources.TryGetValue(c, out int value) ? (float)value : 0f).ToList());

            var visibleValues = visibleIndices.Select(c => allValues[c]).ToList();
            float minValue = Mathf.Min(0f, visibleValues.SelectMany(values => values).DefaultIfEmpty(0f).Min());
            float maxValue = Mathf.Max(1f, visibleValues.SelectMany(values => values).DefaultIfEmpty(0f).Max());
            float valueRange = Mathf.Max(0.001f, maxValue - minValue);

            Vector2 origin = new Vector2(rect.x + padding, rect.y + padding * 0.55f);

            Handles.BeginGUI();
            Handles.color = new Color(0.55f, 0.55f, 0.55f, 0.25f);
            for (int i = 0; i <= 10; i++)
            {
                float x = origin.x + i / 10f * drawWidth;
                Handles.DrawLine(new Vector2(x, origin.y), new Vector2(x, origin.y + drawHeight));
            }

            for (int i = 0; i <= 4; i++)
            {
                float y = origin.y + i / 4f * drawHeight;
                Handles.DrawLine(new Vector2(origin.x, y), new Vector2(origin.x + drawWidth, y));
            }

            for (int c = 0; c < visibleIndices.Count; c++)
            {
                int ci = visibleIndices[c];
                Handles.color = _data.Currencies[ci].Color;
                List<float> values = allValues[ci];
                for (int t = 0; t < tickCount; t++)
                {
                    Vector2 a = GraphPoint(origin, drawWidth, drawHeight, tickCount, t, values[t], minValue, valueRange);
                    Vector2 b = GraphPoint(origin, drawWidth, drawHeight, tickCount, t + 1, values[t + 1], minValue, valueRange);
                    Handles.DrawAAPolyLine(2.2f, a, b);
                }
            }
            Handles.EndGUI();

            GUIStyle label = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.75f, 0.75f, 0.75f) },
                alignment = TextAnchor.MiddleCenter
            };

            for (int i = 0; i <= 4; i++)
            {
                float y = origin.y + i / 4f * drawHeight;
                float value = maxValue - i / 4f * valueRange;
                GUI.Label(new Rect(rect.x + 4, y - 7, 38, 14), value.ToString("F0"), label);
            }

            for (int i = 0; i <= 10; i++)
            {
                float x = origin.x + i / 10f * drawWidth;
                int tickVal = Mathf.RoundToInt(i / 10f * tickCount);
                GUI.Label(new Rect(x - 17, rect.yMax - 18, 34, 14), tickVal.ToString(), label);
            }

            Event evt = Event.current;
            if (evt.type != EventType.Repaint)
                return;

            Vector2 mousePos = evt.mousePosition;
            Rect graphArea = new Rect(origin.x, origin.y, drawWidth, drawHeight);

            if (!graphArea.Contains(mousePos))
                return;

            float tickF = (mousePos.x - origin.x) / drawWidth * tickCount;
            int tickIndex = Mathf.RoundToInt(tickF);
            tickIndex = Mathf.Clamp(tickIndex, 0, tickCount);

            float bestDist = float.MaxValue;
            int bestCurrency = -1;
            float bestValue = 0;

            for (int c = 0; c < visibleIndices.Count; c++)
            {
                int ci = visibleIndices[c];
                List<float> values = allValues[ci];
                float value = values[tickIndex];
                Vector2 point = GraphPoint(origin, drawWidth, drawHeight, tickCount, tickIndex, value, minValue, valueRange);
                float dist = Vector2.Distance(mousePos, point);

                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestCurrency = ci;
                    bestValue = value;
                }
            }

            if (bestCurrency < 0 || bestDist >= 30f)
                return;

            string tooltipText = $"{_data.Currencies[bestCurrency].Name}\nX:{tickIndex}   Y:{bestValue:F0}";

            GUIStyle tooltipStyle = new GUIStyle(EditorStyles.helpBox)
            {
                fontSize = 11,
                normal = { textColor = Color.white },
                alignment = TextAnchor.MiddleCenter
            };
            tooltipStyle.padding = new RectOffset(10, 10, 4, 4);

            GUIContent tooltipContent = new GUIContent(tooltipText);
            Vector2 tooltipSize = tooltipStyle.CalcSize(tooltipContent);
            tooltipSize.x = Mathf.Max(tooltipSize.x * 2, 150f);

            Vector2 tooltipPos = mousePos + new Vector2(14, -tooltipSize.y - 8);
            tooltipPos.x = Mathf.Clamp(tooltipPos.x, rect.x, rect.xMax - tooltipSize.x);
            tooltipPos.y = Mathf.Max(tooltipPos.y, rect.y);

            GUI.Box(new Rect(tooltipPos, tooltipSize), tooltipContent, tooltipStyle);
        }

        private Vector2 GraphPoint(Vector2 origin, float width, float height, int tickCount, int tick, float value, float min, float range)
        {
            return new Vector2(
                origin.x + tick / (float)tickCount * width,
                origin.y + height - (value - min) / range * height);
        }

        private void CreateNewDataInAssets()
        {
            BalancingData newData = CreateInstance<BalancingData>();
            string path = AssetDatabase.GenerateUniqueAssetPath("Assets/BalancingData.asset");
            AssetDatabase.CreateAsset(newData, path);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(newData);
            SetData(newData);
            Selection.activeObject = newData;
        }

        private void CreateNodeAtGraphCenter<T>() where T : BalancingNode
        {
            if (_data == null)
                CreateNewDataInAssets();

            Vector2 center = _graphView != null
                ? _graphView.GetGraphCenterPosition()
                : new Vector2(120, 120);
            CreateNode<T>(center);
        }

        private BalancingNode CreateNode(System.Type nodeType, Vector2 graphPosition)
        {
            if (_data == null || !typeof(BalancingNode).IsAssignableFrom(nodeType))
                return null;

            BalancingNode node = CreateInstance(nodeType) as BalancingNode;
            if (node == null)
                return null;

            node.hideFlags = HideFlags.None;
            node.DisplayName = nodeType.Name.Replace("Node", "");
            node.Position = graphPosition;
            _data.Nodes.Add(node);
            AssetDatabase.AddObjectToAsset(node, _data);
            AssetDatabase.SaveAssets();
            MarkDataDirty();
            _graphView.Populate(_data);
            Selection.activeObject = node;
            return node;
        }

        private T CreateNode<T>(Vector2 graphPosition) where T : BalancingNode
        {
            return CreateNode(typeof(T), graphPosition) as T;
        }

        private void SelectNodeInInspector(BalancingNode node)
        {
            if (node == null)
                return;
            Selection.activeObject = node;
            EditorGUIUtility.PingObject(node);
        }

        private void MarkDataDirty()
        {
            if (_data == null)
                return;
            EditorUtility.SetDirty(_data);
            foreach (BalancingNode node in _data.Nodes)
            {
                if (node != null)
                    EditorUtility.SetDirty(node);
            }
            foreach (Arrow arrow in _data.Arrows)
            {
                if (arrow != null)
                    EditorUtility.SetDirty(arrow);
            }
            AssetDatabase.SaveAssets();
        }

        private sealed class BalanceGraphView : GraphView
        {
            private readonly BalancingWindow _window;
            private readonly Dictionary<string, BalanceNodeView> _nodeViews = new Dictionary<string, BalanceNodeView>();
            private BalancingData _data;
            private bool _isPopulating;
            private bool _isBackgroundPanning;
            private Vector2 _lastMousePosition;

            public event System.Action<BalancingNode> NodeSelected;
            public event System.Action GraphChanged;

            public BalanceGraphView(BalancingWindow window)
            {
                _window = window;
                graphViewChanged = OnGraphViewChanged;

                Insert(0, new GridBackground());
                this.StretchToParentSize();
                SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
                UnityEngine.UIElements.VisualElementExtensions.AddManipulator(this, new ContentDragger());
                UnityEngine.UIElements.VisualElementExtensions.AddManipulator(this, new SelectionDragger());
                UnityEngine.UIElements.VisualElementExtensions.AddManipulator(this, new RectangleSelector());

                RegisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);
                RegisterCallback<MouseMoveEvent>(OnMouseMove, TrickleDown.TrickleDown);
                RegisterCallback<MouseUpEvent>(OnMouseUp, TrickleDown.TrickleDown);
            }

            public void Populate(BalancingData data)
            {
                SaveNodePositions();
                _data = data;

                _isPopulating = true;
                DeleteElements(graphElements.ToList());
                _isPopulating = false;
                _nodeViews.Clear();

                if (_data == null)
                    return;

                foreach (BalancingNode node in _data.Nodes.Where(n => n != null))
                {
                    var view = new BalanceNodeView(node);
                    _nodeViews[node.NodeId] = view;
                    AddElement(view);
                }

                foreach (Arrow arrow in _data.Arrows.ToList())
                {
                    if (arrow == null) continue;
                    if (!_nodeViews.TryGetValue(arrow.FromNodeId, out BalanceNodeView from) ||
                        !_nodeViews.TryGetValue(arrow.ToNodeId, out BalanceNodeView to) ||
                        from.OutputPort == null ||
                        to.InputPort == null)
                        continue;

                    Edge edge = from.OutputPort.ConnectTo(to.InputPort);
                    edge.userData = arrow;
                    AddElement(edge);
                }
            }

            public override void AddToSelection(ISelectable selectable)
            {
                base.AddToSelection(selectable);
                if (selectable is BalanceNodeView nodeView)
                    NodeSelected?.Invoke(nodeView.Node);
                else if (selectable is Edge edge && edge.userData is Arrow arrow)
                    Selection.activeObject = arrow;
            }

            public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
            {
                return ports
                    .Where(port => port != startPort &&
                                   port.node != startPort.node &&
                                   port.direction != startPort.direction)
                    .ToList();
            }

            public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
            {
                base.BuildContextualMenu(evt);

                if (_data == null)
                    return;

                Vector2 graphPosition = this.ChangeCoordinatesTo(contentViewContainer, evt.localMousePosition);
                evt.menu.AppendSeparator();
                evt.menu.AppendAction("Create/Source", _ => _window.CreateNode<SourceNode>(graphPosition));
                evt.menu.AppendAction("Create/Drain", _ => _window.CreateNode<DrainNode>(graphPosition));
                evt.menu.AppendAction("Create/Converter", _ => _window.CreateNode<ConverterNode>(graphPosition));
                evt.menu.AppendAction("Create/Pool", _ => _window.CreateNode<PoolNode>(graphPosition));
            }

            public Vector2 GetGraphCenterPosition()
            {
                return contentViewContainer.WorldToLocal(worldBound.center);
            }

            public void SaveNodePositions()
            {
                if (_data == null)
                    return;

                foreach (BalanceNodeView view in _nodeViews.Values)
                {
                    if (view?.Node == null)
                        continue;
                    view.Node.Position = view.GetPosition().position;
                    EditorUtility.SetDirty(view.Node);
                }
                EditorUtility.SetDirty(_data);
            }

            private GraphViewChange OnGraphViewChanged(GraphViewChange change)
            {
                if (_data == null)
                    return change;

                if (_isPopulating)
                    return change;

                if (change.movedElements != null)
                {
                    foreach (GraphElement element in change.movedElements)
                    {
                        if (element is BalanceNodeView nodeView)
                        {
                            nodeView.Node.Position = nodeView.GetPosition().position;
                            EditorUtility.SetDirty(nodeView.Node);
                        }
                    }
                }

                if (change.edgesToCreate != null)
                {
                    var validEdges = new List<Edge>();
                    foreach (Edge edge in change.edgesToCreate)
                    {
                        if (edge.output?.node is BalanceNodeView from && edge.input?.node is BalanceNodeView to)
                        {
                            Arrow arrow = AddArrow(from.Node, to.Node);
                            if (arrow != null)
                            {
                                edge.userData = arrow;
                                validEdges.Add(edge);
                            }
                        }
                    }
                    change.edgesToCreate = validEdges;
                }

                if (change.elementsToRemove != null)
                {
                    foreach (GraphElement element in change.elementsToRemove)
                    {
                        if (element is Edge edge &&
                            edge.output?.node is BalanceNodeView from &&
                            edge.input?.node is BalanceNodeView to)
                        {
                            RemoveArrow(from.Node, to.Node);
                        }
                        else if (element is BalanceNodeView nodeView)
                        {
                            DeleteNode(nodeView.Node);
                        }
                    }
                }

                EditorUtility.SetDirty(_data);
                AssetDatabase.SaveAssets();
                _window.SyncNodeConnectionLists();
                GraphChanged?.Invoke();
                return change;
            }

            private Arrow AddArrow(BalancingNode from, BalancingNode to)
            {
                if (from == null || to == null || !from.CanHaveOutput || !to.CanHaveInput)
                    return null;

                bool exists = _data.Arrows.Any(a => a != null && a.FromNodeId == from.NodeId && a.ToNodeId == to.NodeId);
                if (exists)
                    return null;

                Arrow arrow = ScriptableObject.CreateInstance<Arrow>();
                arrow.FromNodeId = from.NodeId;
                arrow.ToNodeId = to.NodeId;
                arrow.CurrencyIndex = from.CurrencyIndex;
                arrow.hideFlags = HideFlags.None;
                _data.Arrows.Add(arrow);
                AssetDatabase.AddObjectToAsset(arrow, _data);
                EditorUtility.SetDirty(from);
                EditorUtility.SetDirty(to);
                EditorUtility.SetDirty(arrow);
                AssetDatabase.SaveAssets();
                return arrow;
            }

            private void RemoveArrow(BalancingNode from, BalancingNode to)
            {
                if (from == null || to == null)
                    return;

                Arrow arrow = _data.Arrows.Find(a => a != null && a.FromNodeId == from.NodeId && a.ToNodeId == to.NodeId);
                if (arrow != null)
                {
                    _data.Arrows.Remove(arrow);
                    AssetDatabase.RemoveObjectFromAsset(arrow);
                    DestroyImmediate(arrow, true);
                }

                EditorUtility.SetDirty(from);
                EditorUtility.SetDirty(to);
            }

            private void DeleteNode(BalancingNode node)
            {
                if (node == null)
                    return;

                string nodeId = node.NodeId;

                var arrowsToRemove = _data.Arrows.Where(a => a != null && (a.FromNodeId == nodeId || a.ToNodeId == nodeId)).ToList();
                foreach (Arrow arrow in arrowsToRemove)
                {
                    _data.Arrows.Remove(arrow);
                    AssetDatabase.RemoveObjectFromAsset(arrow);
                    DestroyImmediate(arrow, true);
                }

                _data.Nodes.Remove(node);
                AssetDatabase.RemoveObjectFromAsset(node);
                DestroyImmediate(node, true);
            }

            private void OnMouseDown(MouseDownEvent evt)
            {
                if (evt.button != 0 || (evt.target != this && !(evt.target is GridBackground)))
                    return;

                ClearSelection();
                _isBackgroundPanning = true;
                _lastMousePosition = evt.mousePosition;
                evt.StopPropagation();
            }

            private void OnMouseMove(MouseMoveEvent evt)
            {
                if (!_isBackgroundPanning)
                    return;

                Vector2 delta = evt.mousePosition - _lastMousePosition;
                _lastMousePosition = evt.mousePosition;
                UpdateViewTransform(viewTransform.position + (Vector3)delta, viewTransform.scale);
                evt.StopPropagation();
            }

            private void OnMouseUp(MouseUpEvent evt)
            {
                if (!_isBackgroundPanning || evt.button != 0)
                    return;

                _isBackgroundPanning = false;
                evt.StopPropagation();
            }
        }

        private sealed class BalanceNodeView : Node
        {
            private static readonly Vector2 NodeSize = new Vector2(170, 70);

            public BalancingNode Node { get; }
            public Port InputPort { get; }
            public Port OutputPort { get; }

            public BalanceNodeView(BalancingNode node)
            {
                Node = node;
                title = string.IsNullOrWhiteSpace(node.DisplayName) ? node.NodeType : node.DisplayName;
                viewDataKey = node.NodeId;
                capabilities |= Capabilities.Movable | Capabilities.Deletable | Capabilities.Selectable;

                titleContainer.style.backgroundColor = node.NodeColor * 0.75f;

                if (node.CanHaveInput)
                {
                    InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
                    InputPort.portName = "In";
                    inputContainer.Add(InputPort);
                }

                if (node.CanHaveOutput)
                {
                    OutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
                    OutputPort.portName = "Out";
                    outputContainer.Add(OutputPort);
                }

                var infoContainer = new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.Column,
                        alignItems = Align.Center,
                        marginTop = 2
                    }
                };

                var typeLabel = new Label(node.NodeType)
                {
                    style =
                    {
                        unityTextAlign = TextAnchor.MiddleCenter,
                        color = new Color(0.8f, 0.8f, 0.8f),
                        fontSize = 11
                    }
                };
                infoContainer.Add(typeLabel);

                if (node is PoolNode pool)
                {
                    var poolInfo = new Label("")
                    {
                        style =
                        {
                            unityTextAlign = TextAnchor.MiddleCenter,
                            color = new Color(0.6f, 0.8f, 1f),
                            fontSize = 9,
                            marginTop = 1
                        }
                    };
                    poolInfo.name = "pool-info";
                    infoContainer.Add(poolInfo);
                }

                mainContainer.Add(infoContainer);

                SetPosition(new Rect(node.Position, NodeSize));
                RefreshExpandedState();
                RefreshPorts();
            }
        }
    }
}
