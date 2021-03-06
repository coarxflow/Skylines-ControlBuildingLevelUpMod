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

namespace ControlBuildingLevelMod {
    public class Mod: IUserMod {

        public static string InternalName = "ForceLevel";
        public static string Version = "1.0.1";

        public string Name {
            get { return "Force Building Level"; }
        }
        public string Description {
            get { return "Fully control the level of your growable buildings!"; }
        }
    }
}