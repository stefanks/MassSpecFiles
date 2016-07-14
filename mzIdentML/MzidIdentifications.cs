using MassSpectrometry;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace MzIdentML
{
    public class MzidIdentifications : Identifications
    {
        private mzIdentML.Generated.MzIdentMLType dd;
        public MzidIdentifications(string mzidFile)
        {
            XmlSerializer _indexedSerializer = new XmlSerializer(typeof(mzIdentML.Generated.MzIdentMLType));
            Stream stream = new FileStream(mzidFile, FileMode.Open);
            // Read the XML file into the variable
            dd = _indexedSerializer.Deserialize(stream) as mzIdentML.Generated.MzIdentMLType;
        }

        public double calculatedMassToCharge(int matchIndex)
        {
            return dd.DataCollection.AnalysisData.SpectrumIdentificationList[0].SpectrumIdentificationResult[matchIndex].SpectrumIdentificationItem[0].calculatedMassToCharge;
        }

        public int chargeState(int matchIndex)
        {
            return dd.DataCollection.AnalysisData.SpectrumIdentificationList[0].SpectrumIdentificationResult[matchIndex].SpectrumIdentificationItem[0].chargeState;

        }

        public double experimentalMassToCharge(int matchIndex)
        {
            return dd.DataCollection.AnalysisData.SpectrumIdentificationList[0].SpectrumIdentificationResult[matchIndex].SpectrumIdentificationItem[0].experimentalMassToCharge;
        }

        public int Count()
        {
            return dd.DataCollection.AnalysisData.SpectrumIdentificationList[0].SpectrumIdentificationResult.Count();
        }

        public bool isDecoy(int matchIndex)
        {
            return dd.SequenceCollection.PeptideEvidence[matchIndex].isDecoy;
        }

        public string modificationAcession(int matchIndex, int i)
        {
            return dd.SequenceCollection.Peptide[matchIndex].Modification[i].cvParam[0].accession;
        }

        public string modificationDictionary(int matchIndex, int i)
        {
            return dd.SequenceCollection.Peptide[matchIndex].Modification[i].cvParam[0].cvRef;
        }

        public int modificationLocation(int matchIndex, int i)
        {
            return dd.SequenceCollection.Peptide[matchIndex].Modification[i].location;
        }

        public int NumModifications(int matchIndex)
        {
            if (dd.SequenceCollection.Peptide[matchIndex].Modification == null)
                return 0;
            return dd.SequenceCollection.Peptide[matchIndex].Modification.Length;
        }

        public string PeptideSequenceWithoutModifications(int matchIndex)
        {
            return dd.SequenceCollection.Peptide[matchIndex].PeptideSequence;
        }

        public int ms2spectrumIndex(int matchIndex)
        {
            string ms2spectrumID = dd.DataCollection.AnalysisData.SpectrumIdentificationList[0].SpectrumIdentificationResult[matchIndex].spectrumID;
            return GetLastNumberFromString(ms2spectrumID);
        }

        private static int GetLastNumberFromString(string s)
        {
            return Convert.ToInt32(Regex.Match(s, @"\d+$").Value);
        }

    }
}