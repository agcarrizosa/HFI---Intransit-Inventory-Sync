using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Intransit_Inventory_Sync
{
    class SQLParametersManager
    {
        private String ParamName;
        private PType ParamType;
        private Object ParamValue;
        private long ParaLong;
        public enum PType : int { Varchar = 1, Entero = 2, Money = 3, Bit = 4, Float = 5, Float_Decimal = 6 }

        public SQLParametersManager(String ParamName, PType ParamType, Object ParamValue)
        {
            this.ParamName = ParamName;
            this.ParamType = ParamType;
            this.ParamValue = ParamValue;
        }


        public String ParameterName
        {
            get
            { return this.ParamName; }
            set
            { this.ParamName = value; }
        }

        public PType ParameterType
        {
            get
            { return this.ParamType; }
            set
            { this.ParamType = value; }
        }

        public long Longitud
        {
            get
            { return this.ParaLong; }
            set
            { this.ParaLong = value; }
        }

        public Object ParameterValue
        {
            get
            { return this.ParamValue; }
            set
            { this.ParamValue = value; }
        }
    }
}
