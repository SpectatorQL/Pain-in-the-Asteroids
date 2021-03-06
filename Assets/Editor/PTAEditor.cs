using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace PTA
{
    [CustomPropertyDrawer(typeof(EnumNamedArrayAttribute))]
    public class EnumNamedArrayDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EnumNamedArrayAttribute attrib = (EnumNamedArrayAttribute)attribute;
            
            int onePastLeftBracket = property.propertyPath.LastIndexOf('[') + 1;
            int oneBeforeRightBracket = property.propertyPath.LastIndexOf(']') - 1;
            int length = (oneBeforeRightBracket - onePastLeftBracket) + 1;
            string indexStr = property.propertyPath.Substring(onePastLeftBracket, length);
            
            int index;
            if(int.TryParse(indexStr, out index))
            {
                if(index < attrib.Names.Length)
                {
                    label.text = attrib.Names[index];
                }
                else
                {
                    label.text = "Unknown";
                }
            }
            else
            {
                Debug.Log("EnumNamedArrayDrawer parsing error");
            }
            
            EditorGUI.PropertyField(position, property, label, true);
        }
    }
    
    
    public class PTAEnemyProbabilitySettingsWindow : EditorWindow
    {
        enum View
        {
            Wave,
            Graph
        }
        
        public PTAMain World;
        
        int Wave;
        View DisplayedView;
        
        Vector2 ScrollPosition;
        
        public static void ShowWindow()
        {
            var wnd = (PTAEnemyProbabilitySettingsWindow)EditorWindow.GetWindow(typeof(PTAEnemyProbabilitySettingsWindow));
            if(wnd.World == null)
            {
                wnd.World = FindObjectOfType<PTAMain>();
            }
            wnd.Show();
        }

        void UpdateProbabilityOnSerializationChanges(PTAEnemyProbability enemyProbability, int waveCount, int enemyTypeCount)
        {
            int oldWaveCount = enemyProbability.WaveCount;
            int oldEnemyTypeCount = enemyProbability.EnemyTypeCount;
            float[] oldProbValues = enemyProbability.Values;

            if(oldWaveCount < waveCount)
            {
                float[] newProbValues = new float[waveCount * enemyTypeCount];
                for(int i = 0;
                    i < oldWaveCount * enemyTypeCount;
                    ++i)
                {
                    newProbValues[i] = oldProbValues[i];
                }

                for(int i = oldWaveCount;
                    i < waveCount;
                    ++i)
                {
                    for(int j = 0;
                        j < enemyTypeCount;
                        ++j)
                    {
                        int index = i * enemyTypeCount + j;
                        newProbValues[index] = 0.0f;
                    }
                }

                enemyProbability.WaveCount = waveCount;
                enemyProbability.EnemyTypeCount = enemyTypeCount;
                enemyProbability.Values = newProbValues;
            }
            else if(oldWaveCount > waveCount)
            {
                float[] newProbValues = new float[waveCount * enemyTypeCount];
                for(int i = 0;
                    i < newProbValues.Length;
                    ++i)
                {
                    newProbValues[i] = oldProbValues[i];
                }

                enemyProbability.WaveCount = waveCount;
                enemyProbability.EnemyTypeCount = enemyTypeCount;
                enemyProbability.Values = newProbValues;
            }

            if(oldEnemyTypeCount < enemyTypeCount)
            {
                float[] newProbValues = new float[waveCount * enemyTypeCount];
                for(int i = 0;
                    i < waveCount;
                    ++i)
                {
                    for(int j = 0;
                        j < oldEnemyTypeCount;
                        ++j)
                    {
                        int index = i * oldEnemyTypeCount + j;
                        newProbValues[index] = oldProbValues[index];
                    }
                }

                enemyProbability.WaveCount = waveCount;
                enemyProbability.EnemyTypeCount = enemyTypeCount;
                enemyProbability.Values = newProbValues;
            }
            else if(oldEnemyTypeCount > enemyTypeCount)
            {
                float[] newProbValues = new float[waveCount * enemyTypeCount];
                for(int i = 0;
                    i < waveCount;
                    ++i)
                {
                    for(int j = 0;
                        j < enemyTypeCount;
                        ++j)
                    {
                        int oldValueIndex = i * oldEnemyTypeCount + j;
                        int newValueIndex = i * enemyTypeCount + j;
                        newProbValues[newValueIndex] = oldProbValues[oldValueIndex];
                    }
                }

                enemyProbability.WaveCount = waveCount;
                enemyProbability.EnemyTypeCount = enemyTypeCount;
                enemyProbability.Values = newProbValues;
            }
        }

        // NOTE(SpectatorQL): I know I could've used EditorGUILayout versions of these, I know, I'm dumb.
        // TODO(SpectatorQL): Sane way of doing this kind of stuff !!!
        void OnGUI()
        {
            int waveCount = World.WaveData.MaxWave;
            int enemyTypeCount = (int)EnemyType.Count;

            if((World.EnemyProbability.Values == null) || (World.EnemyProbability.Values.Length == 0))
            {
                World.EnemyProbability.Values = new float[waveCount * enemyTypeCount];
            }

            UpdateProbabilityOnSerializationChanges(World.EnemyProbability, waveCount, enemyTypeCount);


            float firstRowHeight = 24;
            
            Rect waveLabelRect = new Rect(0, 0, 80, firstRowHeight);
            EditorGUI.LabelField(waveLabelRect, $"Wave ({0}-{waveCount - 1})");
            
            Rect waveFieldRect = new Rect(waveLabelRect.width, 0, 32, firstRowHeight);
            Wave = EditorGUI.IntField(waveFieldRect, Wave);
            if(Wave >= waveCount)
            {
                Wave = waveCount - 1;
            }
            else if(Wave < 0)
            {
                Wave = 0;
            }
            
            Vector2 switchViewButtonSize = new Vector2(80, firstRowHeight);
            Vector2 switchViewButtonPosition = new Vector2(position.width - switchViewButtonSize.x, 0);
            Rect switchViewButtonRect = new Rect(switchViewButtonPosition, switchViewButtonSize);
            string switchViewButtonText = "";
            if(DisplayedView == View.Wave)
            {
                switchViewButtonText = "Graph";
            }
            else if(DisplayedView == View.Graph)
            {
                switchViewButtonText = "Wave";
            }
            
            if(GUI.Button(switchViewButtonRect, switchViewButtonText))
            {   
                if(DisplayedView == View.Wave)
                {
                    DisplayedView = View.Graph;
                }
                else if(DisplayedView == View.Graph)
                {
                    DisplayedView = View.Wave;
                }
            }
            
            
            Rect viewRect = new Rect(0, firstRowHeight, position.width, position.height - firstRowHeight);
            EditorGUI.DrawRect(viewRect, Color.gray);
            
            string[] enemyTypeNames = Enum.GetNames(typeof(EnemyType));
            float[] probValues = World.EnemyProbability.Values;
            if(DisplayedView == View.Wave)
            {    
                float rowHeight = 32.0f;
                float rowStepY = 32.0f;
                        
                float enemyTypeLabelX = 4.0f;
                float enemyTypeLabelY = viewRect.position.y + rowStepY;
                float enemyTypeLabelWidth = 64.0f;
                        
                float sliderX = enemyTypeLabelWidth + 4.0f;
                float sliderY = viewRect.position.y + rowStepY;
                float sliderWidth = 256.0f;

                for(int i = 0;
                    i < enemyTypeCount;
                    ++i)
                {
                    EditorGUI.LabelField(new Rect(enemyTypeLabelX, enemyTypeLabelY, enemyTypeLabelWidth, rowHeight), $"{enemyTypeNames[i]}");

                    int currentProbIndex = Wave * enemyTypeCount + i;
                    Debug.Assert(currentProbIndex < probValues.Length);
                    float prob = probValues[currentProbIndex];
                    prob = EditorGUI.Slider(new Rect(sliderX, sliderY, sliderWidth, rowHeight),
                                            prob,
                                            0, PTAEnemyProbability.PROBABILITY_TOTAL);

                    {
                        float probSum = 0.0f;
                        for(int j = 0;
                            j < enemyTypeCount;
                            ++j)
                        {
                            int probIndex = Wave * enemyTypeCount + j;
                            Debug.Assert(probIndex < probValues.Length);
                            probSum += probValues[probIndex];
                        }

                        if(probSum > PTAEnemyProbability.PROBABILITY_TOTAL)
                        {
                            prob = prob - (probSum - PTAEnemyProbability.PROBABILITY_TOTAL);
                            if(prob < 0.0f)
                            {
                                prob = 0.0f;
                            }
                        }
                    }

                    probValues[currentProbIndex] = prob;
                    sliderY += rowStepY;
                    enemyTypeLabelY += rowStepY;
                }
            }
            else if(DisplayedView == View.Graph)
            {
                Rect yAxisRect = new Rect(viewRect.x, viewRect.y, 32, viewRect.height);

                float waveChunkX = yAxisRect.width;
                float waveChunkWidth = 32.0f;
                float graphRectWidth = 0.0f;
                float graphRectHeight = viewRect.height;
                
                Vector2[] waveChunkPositions = new Vector2[waveCount];
                Vector2[] waveChunkSizes = new Vector2[waveCount];
                for(int i = 0;
                    i < waveCount;
                    ++i)
                {
                    waveChunkPositions[i].x = waveChunkX;
                    waveChunkPositions[i].y = viewRect.y;
                    waveChunkSizes[i].x = waveChunkWidth;
                    waveChunkSizes[i].y = graphRectHeight;

                    waveChunkX += waveChunkWidth;
                    graphRectWidth += waveChunkWidth;
                }
                
                Rect graphRect = new Rect(viewRect.x, viewRect.y, graphRectWidth, graphRectHeight);


                ScrollPosition = GUI.BeginScrollView(viewRect, ScrollPosition, graphRect);
                {
                    EditorGUI.DrawRect(yAxisRect, Color.gray);
                    for(float i = 1.0f;
                        i >= 0;
                        i -= 0.1f)
                    {
                        // NOTE(SpectatorQL): Need to do this because labels are drawn in such a way that
                        // the text of a particular label doesn't line up exactly with points on the graph.
                        float labelOffsetY = -8.0f;
                        float y = ((1 - i) * yAxisRect.height) + yAxisRect.y + labelOffsetY;
                        float height = yAxisRect.height / 10.0f;
                        Rect yAxisLabelRect = new Rect(yAxisRect.x, y, yAxisRect.width, height);
                        EditorGUI.LabelField(yAxisLabelRect, $"{i.ToString("0.0")}");
                    }


                    Rect[] waveChunkRects = new Rect[waveCount];
                    for(int i = 0;
                        i < waveCount;
                        ++i)
                    {
                        waveChunkRects[i].x = waveChunkPositions[i].x;
                        waveChunkRects[i].y = waveChunkPositions[i].y;
                        waveChunkRects[i].width = waveChunkSizes[i].x;
                        waveChunkRects[i].height = waveChunkSizes[i].y;
#if true
                        Color debugChunkColor = new Color();
                        debugChunkColor.a = 1.0f;
                        debugChunkColor.r = (float)i / (waveCount - 1);
                        debugChunkColor.g = (float)i / (waveCount - 1);
                        debugChunkColor.b = (float)i / (waveCount - 1);
                        EditorGUI.DrawRect(waveChunkRects[i], debugChunkColor);
#else
                        EditorGUI.DrawRect(waveChunkRects[i], Color.gray);
#endif

                        Rect labelRect = new Rect(waveChunkRects[i].x,
                            waveChunkRects[i].y + waveChunkRects[i].height,
                            waveChunkRects[i].width,
                            viewRect.height - waveChunkRects[i].height);
                        EditorGUI.LabelField(labelRect, $"{i}");
                    }


                    Color[] enemyTypeColors = new Color[enemyTypeCount];
                    for(int i = 0;
                        i < enemyTypeCount;
                        ++i)
                    {
                        enemyTypeColors[i].a = 1.0f;
                        enemyTypeColors[i].r = 1.0f;
                        enemyTypeColors[i].g = (float)i / enemyTypeCount;
                    }

                    Color originalHandlesColor = Handles.color;
                    for(int i = 0;
                        i < waveCount - 1;
                        ++i)
                    {
                        for(int j = 0;
                            j < enemyTypeCount;
                            ++j)
                        {
                            Handles.color = enemyTypeColors[j];

                            int probIndex = i * enemyTypeCount + j;
                            int nextProbIndex = probIndex + enemyTypeCount;
                            Debug.Assert(probIndex < probValues.Length);

                            // NOTE(SpectatorQL): For down-to-up drawing.
                            float f1 = 1 - probValues[probIndex];
                            float f2 = 1 - probValues[nextProbIndex];

                            float xOffset = 4.5f;
                            float x1 = waveChunkRects[i].x + xOffset;
                            float x2 = waveChunkRects[i + 1].x + xOffset;
                            float y1 = (waveChunkRects[i].height * f1) + waveChunkRects[i].y;
                            float y2 = (waveChunkRects[i + 1].height * f2) + waveChunkRects[i + 1].y;
                            Vector2 currentWaveProbPoint = new Vector2(x1, y1);
                            Vector2 nextWaveProbPoint = new Vector2(x2, y2);

                            Handles.DrawLine(currentWaveProbPoint, nextWaveProbPoint);
                            float pointSize = 3.25f;
                            Handles.CircleHandleCap(0, currentWaveProbPoint, Quaternion.identity, pointSize, EventType.Repaint);
                            Handles.CircleHandleCap(0, nextWaveProbPoint, Quaternion.identity, pointSize, EventType.Repaint);
                        }
                    }
                    Handles.color = originalHandlesColor;
                }
                GUI.EndScrollView();
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
    }
    
    [CustomPropertyDrawer(typeof(PTAEnemyProbability))]
    public class PTAEnemyProbabilityDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            if(GUI.Button(position, "Enemy probability settings..."))
            {
                PTAEnemyProbabilitySettingsWindow.ShowWindow();
            }
            
            EditorGUI.EndProperty();
        }
    }
}
