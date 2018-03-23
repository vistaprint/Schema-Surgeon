using System.Collections.Generic;
using SchemaSurgeon.ModifyColumns.Constraints;

namespace SchemaSurgeon.ModifyColumns
{
    public class AlterColumnTypeDetails
    {
        public AlterColumnTypeDetails(IList<ConstraintDetail> affectedConstraintDetails, IList<ColumnDetail> affectedColumnDetails)
        {
            AffectedConstraintDetails = affectedConstraintDetails;
            AffectedColumnDetails = affectedColumnDetails;
        }

        public readonly IList<ConstraintDetail> AffectedConstraintDetails;
        public readonly IList<ColumnDetail> AffectedColumnDetails;
    }
}
