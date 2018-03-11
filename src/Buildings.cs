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

using ICities;
using System.Collections.Generic;
using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System;
using UnityEngine;

namespace BuildingStates {
    public static class Buildings {

        // based on SimulationStepActive from PrivateBuildingAI
        public static void AbandonBuilding(ushort buildingID)
        {
            Building b = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID];

            b.m_flags &= ~Building.Flags.Active;
            b.m_flags |= Building.Flags.Abandoned;
            //b.m_problems = (Notification.Problem.FatalProblem | (b.m_problems & ~Notification.Problem.MajorProblem));

            //base.RemovePeople(buildingID, ref buildingData, 100);
            InstanceID empty = InstanceID.Empty;
            empty.Building = buildingID;
            InstanceManager.Group group = Singleton<InstanceManager>.instance.GetGroup(empty);
            DisasterHelpers.RemovePeople(group, buildingID, ref b.m_citizenUnits, 100, ref Singleton<SimulationManager>.instance.m_randomizer);

            b.Info.m_buildingAI.BuildingDeactivated(buildingID, ref b);
            Singleton<BuildingManager>.instance.UpdateBuildingRenderer(buildingID, true);

            Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID] = b;
        }

        public static void CollapseBuilding(ushort buildingID)
        {
            Building b = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID];

            b.m_flags &= ~Building.Flags.Abandoned;

            b.Info.m_buildingAI.CollapseBuilding(buildingID, ref b, null, false, false, 0);

            Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID] = b;
        }

        public static void ClearProblems(ushort buildingID)
        {
            Building b = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID];

            Notification.Problem problems = b.m_problems;
            b.m_problems = Notification.Problem.None;
            if (b.m_problems != problems)
            {
                Singleton<BuildingManager>.instance.UpdateNotifications(buildingID, problems, b.m_problems);
            }
        }

        public static void RestoreBuilding(ushort buildingID)
        {
            Building b = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID];

            b.m_flags &= ~(Building.Flags.Abandoned | Building.Flags.Collapsed);
            b.m_flags |= Building.Flags.Active;

            /*if((b.m_flags & Building.Flags.Collapsed) != Building.Flags.None) //rebuild animation
            {
                Building.Frame frame = b.GetLastFrameData();
                frame.m_constructState = 0;
                b.m_flags |= Building.Flags.Upgrading;
                b.SetLastFrameData(frame);
            }*/
            
            Notification.Problem problems = b.m_problems;
            b.m_problems &= ~(Notification.Problem.StructureDamaged | Notification.Problem.FatalProblem);
            if (b.m_problems != problems)
            {
                Singleton<BuildingManager>.instance.UpdateNotifications(buildingID, problems, b.m_problems);
            }

            Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID] = b;
        }

    }
}