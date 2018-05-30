/*
 
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at http://mozilla.org/MPL/2.0/.
 
  Copyright (C) 2009-2012 Michael Möller <mmoeller@openhardwaremonitor.org>
	
*/

using System;
using System.Text;
using OpenHardwareMonitor.Hardware.LPC;

namespace OpenHardwareMonitor.Hardware.Mainboard
{
    internal class Mainboard : IHardware
    {
        private readonly LMSensors lmSensors;
        private readonly LPCIO lpcio;
        private readonly string name;
        private readonly ISettings settings;
        private readonly SMBIOS smbios;
        private readonly Hardware[] superIOHardware;
        private string customName;

        public Mainboard(SMBIOS smbios, ISettings settings)
        {
            this.settings = settings;
            this.smbios = smbios;

            var manufacturer = smbios.Board == null
                ? Manufacturer.Unknown
                : Identification.GetManufacturer(smbios.Board.ManufacturerName);

            var model = smbios.Board == null ? Model.Unknown : Identification.GetModel(smbios.Board.ProductName);

            if (smbios.Board != null)
            {
                if (!string.IsNullOrEmpty(smbios.Board.ProductName))
                {
                    if (manufacturer == Manufacturer.Unknown)
                        name = smbios.Board.ProductName;
                    else
                        name = manufacturer + " " +
                               smbios.Board.ProductName;
                }
                else
                {
                    name = manufacturer.ToString();
                }
            }
            else
            {
                name = Manufacturer.Unknown.ToString();
            }

            customName = settings.GetValue(
                new Identifier(Identifier, "name").ToString(), name);

            ISuperIO[] superIO;

            lpcio = new LPCIO();
            superIO = lpcio.SuperIO;

            superIOHardware = new Hardware[superIO.Length];
            for (var i = 0; i < superIO.Length; i++)
                superIOHardware[i] = new SuperIOHardware(this, superIO[i],
                    manufacturer, model);
        }

        public string Name
        {
            get => customName;
            set
            {
                if (!string.IsNullOrEmpty(value))
                    customName = value;
                else
                    customName = name;
                settings.SetValue(new Identifier(Identifier, "name").ToString(),
                    customName);
            }
        }

        public Identifier Identifier => new Identifier("mainboard");

        public HardwareType HardwareType => HardwareType.Mainboard;

        public virtual IHardware Parent => null;

        public string GetReport()
        {
            var r = new StringBuilder();

            r.AppendLine("Mainboard");
            r.AppendLine();
            r.Append(smbios.GetReport());

            if (lpcio != null)
                r.Append(lpcio.GetReport());

            var table =
                FirmwareTable.GetTable(FirmwareTable.Provider.ACPI, "TAMG");
            if (table != null)
            {
                var tamg = new GigabyteTAMG(table);
                r.Append(tamg.GetReport());
            }

            return r.ToString();
        }

        public void Update()
        {
        }

        public IHardware[] SubHardware => superIOHardware;

        public ISensor[] Sensors => new ISensor[0];

        public void Accept(IVisitor visitor)
        {
            if (visitor == null)
                throw new ArgumentNullException(nameof(visitor));
            visitor.VisitHardware(this);
        }

        public void Traverse(IVisitor visitor)
        {
            foreach (IHardware hardware in superIOHardware)
                hardware.Accept(visitor);
        }

        public void Close()
        {
            if (lmSensors != null)
                lmSensors.Close();
            foreach (var hardware in superIOHardware)
                hardware.Close();
        }

#pragma warning disable 67
        public event SensorEventHandler SensorAdded;
        public event SensorEventHandler SensorRemoved;
#pragma warning restore 67
    }
}