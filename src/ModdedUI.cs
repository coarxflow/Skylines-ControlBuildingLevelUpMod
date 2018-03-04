/*
    Copyright (c) 2015, Max Stark <max.stark88@web.de> 
        All rights reserved.
    
    This file is part of ControlBuildingLevelUpMod, which is free 
    software: you can redistribute it and/or modify it under the terms 
    of the GNU General Public License as published by the Free 
    Software Foundation, either version 2 of the License, or (at your 
    option) any later version.
    
    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
    General Public License for more details. 
    
    You should have received a copy of the GNU General Public License 
    along with this program; if not, see <http://www.gnu.org/licenses/>.
*/

using ColossalFramework;
using ColossalFramework.UI;
using ICities;
using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

namespace BuildingStates {
    public class ModdedUI : MonoBehaviour {

        ZonedBuildingWorldInfoPanel m_zonedBuildingInfoPanel;
        BuildingWorldInfoPanel m_generalBuildingInfoPanel;
        EventBuildingWorldInfoPanel m_eventBuildingInfoPanel;

        ushort m_currentSelectedBuildingID;

        public void Install()
        {
            m_zonedBuildingInfoPanel = GameObject.Find("(Library) ZonedBuildingWorldInfoPanel").GetComponent<ZonedBuildingWorldInfoPanel>();
            m_generalBuildingInfoPanel = GameObject.Find("(Library) CityServiceWorldInfoPanel").GetComponent<BuildingWorldInfoPanel>();
            m_eventBuildingInfoPanel = GameObject.Find("(Library) FootballPanel").GetComponent<EventBuildingWorldInfoPanel>();

            this.installInPanel(m_zonedBuildingInfoPanel);
            if(m_generalBuildingInfoPanel != null)
                this.installInPanel(m_generalBuildingInfoPanel);
            if(m_eventBuildingInfoPanel != null)
                this.installInPanel(m_eventBuildingInfoPanel);

        }

        private void installInPanel(WorldInfoPanel panel)
        {
            // Clone template and keep only dropdown
            UIComponent optionsDropdownTemplate = UITemplateManager.Get("OptionsDropdownTemplate"); ;
            UIDropDown statesDropDown = optionsDropdownTemplate.Find<UIDropDown>("Dropdown");

            // positioning
            panel.component.AttachUIComponent(statesDropDown.gameObject);
            optionsDropdownTemplate.AlignTo(panel.component, UIAlignAnchor.TopLeft);
            optionsDropdownTemplate.relativePosition += new Vector3(100f, 130f, 0f);
            /*UIComponent progressBar = panel.component.Find("LevelProgress");
            if(progressBar != null)
            {
                //statesDropDown.AlignTo(panel.component, UIAlignAnchor.TopLeft);
                statesDropDown.transform.position += new Vector3(200f, 200f, 0f);
            } else {
                //statesDropDown.AlignTo(panel.component, UIAlignAnchor.TopRight);
                statesDropDown.relativePosition += new Vector3(200f, 80f, 0f);
            }*/

            // Styling
            statesDropDown.name = "BuildingStateDropDown";
            statesDropDown.height = 20f;
            statesDropDown.width = 100f;
            statesDropDown.triggerButton.size = new Vector2(100f,20f);
            //statesDropDown.textScale = 0.5f;
            //statesDropDown.textFieldPadding = new RectOffset(0,0,0,0);

            // States list
            statesDropDown.AddItem("Active");
            statesDropDown.AddItem("Abandoned");
            statesDropDown.AddItem("Collapsed");

            statesDropDown.tooltip = "Pick state of the building";

            statesDropDown.eventSelectedIndexChanged += (e, index) =>
            {
                switch (index)
                {
                    case 0:
                        Buildings.RestoreBuilding(this.getSelectedBuildingID(panel));
                        break;
                    case 1:
                        Buildings.AbandonBuilding(this.getSelectedBuildingID(panel));
                        break;
                    case 2:
                        Buildings.CollapseBuilding(this.getSelectedBuildingID(panel));
                        break;
                    default:
                        break;
                }
            };

            UpdateStatesDropDown callback = (flags) => {
                if ((flags & Building.Flags.Abandoned) != (Building.Flags)0)
                {
                    statesDropDown.selectedIndex = 1;
                }
                else if ((flags & Building.Flags.Collapsed) != (Building.Flags)0)
                {
                    statesDropDown.selectedIndex = 2;
                }
                else
                {
                    statesDropDown.selectedIndex = 0;
                }
            };

            panel.component.eventPositionChanged += (inst1, inst2) =>
            {
                if (m_zonedBuildingInfoPanel.component.isVisible)
                    OnSelected(panel, callback);
            };

            panel.component.eventOpacityChanged += (inst1, inst2) =>
            {
                if (m_zonedBuildingInfoPanel.component.isVisible)
                    OnSelected(panel, callback);
            };
        }

        private delegate void UpdateStatesDropDown(Building.Flags flags);

        void OnSelected(WorldInfoPanel panel, UpdateStatesDropDown callback)
        {
            //get selected building ID (after waiting it has been actualized)
            FieldInfo baseSub = panel.GetType().GetField("m_InstanceID", BindingFlags.NonPublic | BindingFlags.Instance);
            InstanceID instanceId = (InstanceID)baseSub.GetValue(m_zonedBuildingInfoPanel);
            if (instanceId.Type == InstanceType.Building && instanceId.Building != 0)
            {
                if (m_currentSelectedBuildingID == instanceId.Building) //no update needed
                    return;
                m_currentSelectedBuildingID = instanceId.Building;

                Building build = Singleton<BuildingManager>.instance.m_buildings.m_buffer[m_currentSelectedBuildingID];
                callback(build.m_flags);
            }
            else
            {
                m_currentSelectedBuildingID = 0;
            }
        }

        private ushort getSelectedBuildingID(WorldInfoPanel panel) {
            return Convert.ToUInt16(
                    this.getProperty("Index", this.getField("m_InstanceID", panel))
                    .ToString());
        }

        private System.Object getField(String name, System.Object obj) {
            MemberInfo[] members = obj.GetType().GetMembers(BindingFlags.Instance |
                                                            BindingFlags.NonPublic |
                                                            BindingFlags.Public |
                                                            BindingFlags.Static);
            foreach (MemberInfo member in members) {
                if (member.MemberType == MemberTypes.Field) {
                    FieldInfo field = (FieldInfo)member;
                    if (field.Name.Equals(name)) {
                        return field.GetValue(obj);
                    }
                }
            }

            return null;
        }

        private System.Object getProperty(String name, System.Object obj) {
            MemberInfo[] members = obj.GetType().GetMembers(BindingFlags.Instance |
                                                            BindingFlags.NonPublic |
                                                            BindingFlags.Public |
                                                            BindingFlags.Static);
            foreach (MemberInfo member in members) {
                if (member.MemberType == MemberTypes.Property) {
                    PropertyInfo property = (PropertyInfo)member;
                    if (property.Name.Equals(name)) {
                        return property.GetValue(obj, null);
                    }
                }
            }

            return null;
        }
    }
}