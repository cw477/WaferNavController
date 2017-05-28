using System.Data;
using System.Data.Linq;
using System.Data.Linq.Mapping;

namespace WaferNavController.DatabaseAndMqtt {

    [Database]
    public class WaferNavDatabase : DataContext {
        public WaferNavDatabase(string fileOrServerOrConnection) : base(fileOrServerOrConnection) {
        }

        public WaferNavDatabase(string fileOrServerOrConnection, MappingSource mapping) : base(fileOrServerOrConnection, mapping) {
        }

        public WaferNavDatabase(IDbConnection connection) : base(connection) {
        }

        public WaferNavDatabase(IDbConnection connection, MappingSource mapping) : base(connection, mapping) {
        }

        public Table<BLU> BLUs;
        public Table<blu_assignment_load> bluLoadAssignments;
        public Table<blu_assignment_unload> bluUnloadAssignments;
        public Table<SLT> SLTs;
        public Table<active_bib> activeBibs;
        public Table<slt_assignment> sltAssignments;
    }

    [Table(Name = "wn.BLU")]
    public class BLU {
        [Column(IsPrimaryKey = true)]
        public string id;
        [Column]
        public bool available;
        [Column]
        public string site_name;
        [Column]
        public string site_description;
        [Column]
        public string site_location;
    }

    [Table(Name = "wn.SLT")]
    public class SLT {
        [Column(IsPrimaryKey = true)]
        public string id;
        [Column]
        public bool available;
        [Column]
        public string site_name;
        [Column]
        public string site_description;
        [Column]
        public string site_location;
    }

    [Table(Name = "wn.active_bib")]
    public class active_bib {
        [Column(IsPrimaryKey = true)]
        public string id;
    }

    [Table(Name = "wn.blu_assignment_load")]
    public class blu_assignment_load {
        [Column(IsPrimaryKey = true)]
        public string blu_id;
        [Column]
        public string wafer_type_id;
    }

    [Table(Name = "wn.slt_assignment")]
    public class slt_assignment {
        [Column(IsPrimaryKey = true)]
        public string slt_id;
        [Column]
        public string bib_id;
    }

    [Table(Name = "wn.blu_assignment_unload")]
    public class blu_assignment_unload {
        [Column(IsPrimaryKey = true)]
        public string blu_id;
        [Column]
        public string bib_id;
    }
}
