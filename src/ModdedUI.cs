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
        EventBuildingWorldInfoPanel m_footballBuildingInfoPanel;
        EventBuildingWorldInfoPanel m_concertBuildingInfoPanel;

        ushort m_currentSelectedBuildingID;

        public void Install()
        {
            m_zonedBuildingInfoPanel = GameObject.Find("(Library) ZonedBuildingWorldInfoPanel").GetComponent<ZonedBuildingWorldInfoPanel>();
            m_generalBuildingInfoPanel = GameObject.Find("(Library) CityServiceWorldInfoPanel").GetComponent<BuildingWorldInfoPanel>();
            m_footballBuildingInfoPanel = GameObject.Find("(Library) FootballPanel").GetComponent<EventBuildingWorldInfoPanel>();
            m_concertBuildingInfoPanel = GameObject.Find("(Library) FestivalPanel").GetComponent<EventBuildingWorldInfoPanel>();
            WorldInfoPanel[] wip = GameObject.FindObjectsOfType<EventBuildingWorldInfoPanel>();
            foreach (var item in wip)
            {
                CODebug.Log(LogChannel.Modding, "WIP "+ item.name);
            }

            this.installInPanel(m_zonedBuildingInfoPanel, 0);
            if(m_generalBuildingInfoPanel != null)
                this.installInPanel(m_generalBuildingInfoPanel, 1);
            if(m_footballBuildingInfoPanel != null)
                this.installInPanel(m_footballBuildingInfoPanel, 2);
            if (m_concertBuildingInfoPanel != null)
                this.installInPanel(m_concertBuildingInfoPanel, 2);

        }

        private void installInPanel(WorldInfoPanel panel, ushort positionCase)
        {
            int spriteWidth = 32;
            int spriteHeight = 32;
            string[] tooltips = {
                 "Click to change building state",
                 "Building Abandoned",
                 "Building Collapsed"
            };

            StatesButton statesButton = new StatesButton(panel.component, spriteWidth, spriteHeight, 3, "icons.states.png", "BuildingStates", tooltips);
            
            statesButton.msb.eventActiveStateIndexChanged += (component, value) => {

                if (!m_allowEvents)
                    return;

                switch (value)
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
            statesButton.msb.AlignTo(panel.component, UIAlignAnchor.TopRight);
            switch(positionCase)
            {
                case 0: //Zoned Building
                    statesButton.msb.relativePosition += new Vector3(-75f, 80f, 0f);
                    break;
                case 1: //Service Building
                    statesButton.msb.relativePosition += new Vector3(-50f, 80f, 0f);
                    break;
                case 2: //Event Building
                    statesButton.msb.relativePosition += new Vector3(-50f, 90f, 0f);
                    break;
            }

            UpdateStatesDropDown callback = (flags) => {
                if ((flags & Building.Flags.Abandoned) != (Building.Flags)0)
                {
                    statesButton.SetState(1);
                }
                else if ((flags & Building.Flags.Collapsed) != (Building.Flags)0)
                {
                    statesButton.SetState(2);
                }
                else
                {
                    statesButton.SetState(0);
                }
            };

            panel.component.eventPositionChanged += (inst1, inst2) =>
            {
                if (panel.component.isVisible)
                    OnSelected(panel, callback);
            };

            panel.component.eventOpacityChanged += (inst1, inst2) =>
            {
                if (panel.component.isVisible)
                    OnSelected(panel, callback);
            };
        }

        private delegate void UpdateStatesDropDown(Building.Flags flags);

        bool m_allowEvents = true;

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
                m_allowEvents = false;
                callback(build.m_flags);
                m_allowEvents = true;
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