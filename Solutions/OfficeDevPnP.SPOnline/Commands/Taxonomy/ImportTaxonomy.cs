﻿using OfficeDevPnP.SPOnline.CmdletHelpAttributes;
using OfficeDevPnP.SPOnline.Core;
using OfficeDevPnP.SPOnline.Commands.Base;
using System.Management.Automation;

namespace OfficeDevPnP.SPOnline.Commands
{
    [Cmdlet(VerbsData.Import, "SPOTaxonomy", SupportsShouldProcess = true)]
    [CmdletHelp("Imports a taxonomy from either a string array or a file")]
    [CmdletExample(Code = @"
PS:> Import-SPOTaxonomy -Terms 'Company|Locations|Stockholm'",
           Remarks = "Creates a new termgroup, 'Company', a termset 'Locations' and a term 'Stockholm'")]
    [CmdletExample(Code = @"
PS:> Import-SPOTaxonomy -Terms 'Company|Locations|Stockholm|Central','Company|Locations|Stockholm|North'",
       Remarks = "Creates a new termgroup, 'Company', a termset 'Locations', a term 'Stockholm' and two subterms: 'Central', and 'North'")]
    public class ImportTaxonomy : SPOCmdlet
    {

        [Parameter(Mandatory = false, ValueFromPipeline = true, ParameterSetName = "Direct", HelpMessage = "An array of strings describing termgroup, termset, term, subterms using a default delimiter of '|'.")]
        public string[] Terms;

        [Parameter(Mandatory = true, ParameterSetName = "File", HelpMessage = "Specifies a file containing terms per line, in the format as required by the Terms parameter.")]
        public string Path;

        [Parameter(Mandatory = false, ParameterSetName = ParameterAttribute.AllParameterSets)]
        public int LCID = 1033;

        [Parameter(Mandatory = false, ParameterSetName = ParameterAttribute.AllParameterSets)]
        public string Delimiter = "|";

        protected override void ExecuteCmdlet()
        {
            string[] lines = null;
            if (ParameterSetName == "File")
            {
                lines = System.IO.File.ReadAllLines(Path);
            }
            else
            {
                lines = Terms;
            }
            SPOTaxonomy.ImportTerms(lines, LCID, Delimiter, ClientContext);
        }

    }
}
