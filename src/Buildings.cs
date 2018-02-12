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
        public const int RESIDENTIAL = 0;
        public const int COMMERCIAL  = 1;
        public const int INDUSTRIAL  = 2;
        public const int OFFICE      = 3;

        /* 
        Since there are no concurrent collections in .NET 3.5 we have to use old-style locking 
        private static ConcurrentDictionary<ushort, Level> buildings = new ConcurrentDictionary<ushort, Level>();
        */
        private static Dictionary<ushort, Level> buildings = new Dictionary<ushort, Level>();
        private static System.Object lockBuilding = new System.Object();

        public static void fromByteArray(byte[] data) {
            if (data != null) {
                BinaryFormatter bFormatter = new BinaryFormatter();
                MemoryStream mStream       = new MemoryStream(data);
                lock (lockBuilding) { 
                    buildings = (Dictionary<ushort, Level>)bFormatter.Deserialize(mStream);
                }

            } else {
                lock (lockBuilding) { 
                    buildings = new Dictionary<ushort, Level>();
                }
            }
        }

        public static byte[] toByteArray() {
            BinaryFormatter bFormatter = new BinaryFormatter();
            MemoryStream mStream       = new MemoryStream();
            bFormatter.Serialize(mStream, buildings);

            return mStream.ToArray();
        }

        public static void add(ushort buildingID, Level level) {
            lock (lockBuilding) { 
                buildings.Add(buildingID, level);
            }
        } 

        public static void remove(ushort buildingID) {
            lock (lockBuilding) { 
                buildings.Remove(buildingID);
            }
        }

        public static Byte getDistrictID(ushort buildingID) {
            BuildingManager buildingManager = Singleton<BuildingManager>.instance;
            DistrictManager districtManager = Singleton<DistrictManager>.instance;
            
            return districtManager.GetDistrict(
                   buildingManager.m_buildings.m_buffer[buildingID].m_position);
        }

        public static int getBuildingType(ushort buildingID) {
            BuildingManager buildingManager = Singleton<BuildingManager>.instance;
            Building building = buildingManager.m_buildings.m_buffer[buildingID];
            ItemClass.Service buildingType = building.Info.m_class.m_service;

            switch (buildingType) {
                case ItemClass.Service.Residential: return RESIDENTIAL;
                case ItemClass.Service.Commercial: return COMMERCIAL;
                case ItemClass.Service.Industrial: return INDUSTRIAL;
                case ItemClass.Service.Office: return OFFICE;
                default: return -1;
            }
        }

        /** 
         *   methods added by CoarzFlovv
         **/

        //Method based on GetUpgradeInfo from PrivateBuildingAI
        public static BuildingInfo GetInfoForLevel(ushort buildingID, ref Building data, ItemClass.Level level)
        {
            Randomizer randomizer;

            //fully randomize first level building
            if(level == 0)
            {
                randomizer = Singleton<SimulationManager>.instance.m_randomizer;
            }
            else //building based on level and id, as in original code
            {
                randomizer = new Randomizer((int)buildingID);
                for (int i = 0; i <= (int)(level - 1); i++)
                {
                    randomizer.Int32(1000u);
                }
            }



            DistrictManager instance = Singleton<DistrictManager>.instance;
            byte district = instance.GetDistrict(data.m_position);
            ushort style = instance.m_districts.m_buffer[(int)district].m_Style;
            return Singleton<BuildingManager>.instance.GetRandomBuildingInfo(ref randomizer, data.Info.m_buildingAI.m_info.m_class.m_service, data.Info.m_buildingAI.m_info.m_class.m_subService, level, data.Width, data.Length, data.Info.m_buildingAI.m_info.m_zoningMode, (int)style);
        }

        // Method based on UpgradeBuilding from BuildingManager
        public static bool ForceLevelBuilding(ushort buildingID, Level level)
        {

            BuildingManager instance = Singleton<BuildingManager>.instance;
            if (buildingID == 0 || (instance.m_buildings.m_buffer[(int)buildingID].m_flags & Building.Flags.Created) == Building.Flags.None)
            {
                return false;
            }
            BuildingInfo info = instance.m_buildings.m_buffer[(int)buildingID].Info;
            if (info == null)
            {
                return false;
            }

            if ((Level) info.m_class.m_level == level)
            {
                return false;
            }

            BuildingInfo upgradeInfo = Buildings.GetInfoForLevel(buildingID, ref instance.m_buildings.m_buffer[(int)buildingID], (ItemClass.Level) level);
            if (upgradeInfo == null || upgradeInfo == info)
            {
                return false;
            }
            instance.UpdateBuildingInfo(buildingID, upgradeInfo);
            upgradeInfo.m_buildingAI.BuildingUpgraded(buildingID, ref instance.m_buildings.m_buffer[(int)buildingID]);
            int constructionCost = upgradeInfo.m_buildingAI.GetConstructionCost();
            Singleton<EconomyManager>.instance.FetchResource(EconomyManager.Resource.Construction, constructionCost, upgradeInfo.m_class);
            int num = 0;
            while (buildingID != 0)
            {
                BuildingInfo info2 = instance.m_buildings.m_buffer[(int)buildingID].Info;
                if (info2 != null)
                {
                    Vector3 position = instance.m_buildings.m_buffer[(int)buildingID].m_position;
                    float angle = instance.m_buildings.m_buffer[(int)buildingID].m_angle;
                    BuildingTool.DispatchPlacementEffect(info2, 0, position, angle, info2.m_cellWidth, info2.m_cellLength, false, false);
                }
                buildingID = instance.m_buildings.m_buffer[(int)buildingID].m_subBuilding;
                if (++num > 49152)
                {
                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                    break;
                }
            }

            return true;
        }

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

            b.Info.m_buildingAI.CollapseBuilding(buildingID, ref b, null, false, false, 0);

            Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID] = b;
        }

        public static void RestoreBuilding(ushort buildingID)
        {
            Building b = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID];

            b.m_flags &= ~(Building.Flags.Abandoned | Building.Flags.Collapsed);
            b.m_flags |= Building.Flags.Active;

            Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID] = b;
        }

        public static void dump() {
            int i = 0;
            foreach (KeyValuePair<ushort, Level> building in buildings) {
                i++;
                Logger.Info("(" + i + ") building " + building.Key + " has lock-level " + building.Value);
            }
        }
    }
}