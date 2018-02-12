﻿/*
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
    public class LoadingExtension : LoadingExtensionBase {

        ZonedBuildingWorldInfoPanel m_zonedBuildingInfoPanel;
        BuildingWorldInfoPanel m_generalBuildingInfoPanel;
        EventBuildingWorldInfoPanel m_eventBuildingInfoPanel;

        ushort m_currentSelectedBuildingID;

        public override void OnLevelLoaded(LoadMode mode)
        {
            if (mode != LoadMode.LoadGame && mode != LoadMode.NewGame)
                return;

            this.Install();
        }

        public override void OnLevelUnloading()
        {
            

        }

        public override void OnReleased() {
           
        }

        private void Install()
        {
            m_zonedBuildingInfoPanel = GameObject.Find("(Library) ZonedBuildingWorldInfoPanel").GetComponent<ZonedBuildingWorldInfoPanel>();

            this.installInPanel(m_zonedBuildingInfoPanel);

        }

        private void installInPanel(WorldInfoPanel panel)
        {
            UIDropDown statesDropDown = panel.component.AddUIComponent<UIDropDown>();
            statesDropDown.AddItem("-");
            statesDropDown.AddItem("abandoned");
            statesDropDown.AddItem("collapsed");

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

            UpdateStatesDropDown callback = (flags) => { this.ZonedUpdateStatesDropDown(flags, statesDropDown); };

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

        void ZonedUpdateStatesDropDown(Building.Flags flags, UIDropDown dropDown)
        {
            if((flags & Building.Flags.Abandoned) != (Building.Flags) 0)
            {
                dropDown.selectedIndex = 1;
            }
            else if ((flags & Building.Flags.Collapsed) != (Building.Flags)0)
            {
                dropDown.selectedIndex = 2;
            }
            else
            {
                dropDown.selectedIndex = 0;
            }
        }

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