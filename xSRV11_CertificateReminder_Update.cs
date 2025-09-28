// 1. Filter only records where "ToPrint" is true
var vToPrintList = queryResultDataset.Results
    .Where(r => r.Calculated_ToPrint == true)
    .ToList();

// 2. Get distinct customers from the filtered list
var distinctCustomers = vToPrintList
    .Select(r => r.Customer_CustID) // Replace with the actual customer field name
    .Distinct()
    .ToList();

string sMessage = "";
foreach (var custID in distinctCustomers)
{
    // 3. Retrieve all certificates for the current customer where "ToPrint" is true
    var certList = vToPrintList
        .Where(r => r.Customer_CustID == custID)
        .Select(r => r.EquipPlan_xLMCertificateNum_c) // Replace with the actual certificate number field name
        .ToList();
        
    var getEmail = vToPrintList
        .Where(r => r.Customer_CustID == custID)
        .Select(r => r.Calculated_Email)
        .FirstOrDefault();
        
    var getName = vToPrintList
        .Where(r => r.Customer_CustID == custID)
        .Select(r => r.Customer_Name)
        .FirstOrDefault();
        

    // 4. Combine all certificate numbers into a single string using "~" as the delimiter
    var combinedCerts = string.Join("~", certList);

   try {
      
      Ice.Tablesets.DynamicCriteriaTableset vRptSet = new Ice.Tablesets.DynamicCriteriaTableset();
      Ice.Contracts.DynamicCriteriaSvcContract vRptSvc = Ice.Assemblies.ServiceRenderer.GetService<Ice.Contracts.DynamicCriteriaSvcContract>(Db);
      vRptSvc.GetNewDynamicCriteriaReportParam("XSRV1102");
      
      var styleRow = vRptSet.ReportStyle.NewRow();
      vRptSet.ReportStyle.Add(styleRow);
      vRptSet.ReportStyle[0].Company = "WFEP1";
      vRptSet.ReportStyle[0].ReportID = "XSRV1102";
      vRptSet.ReportStyle[0].StyleNum = 1001;
      vRptSet.ReportStyle[0].StyleDescription = "LM Reminder Letter";
      vRptSet.ReportStyle[0].RptTypeID = "SSRS";
      vRptSet.ReportStyle[0].PrintProgram = "reports/CustomReports/XSRV1102/LMReminderLetter";
      vRptSet.ReportStyle[0].PrintProgramOptions = "";
      vRptSet.ReportStyle[0].RptDefID = "XSRV1102";
      vRptSet.ReportStyle[0].CompanyList = "WFEP1";
      vRptSet.ReportStyle[0].ServerNum = 0;
      vRptSet.ReportStyle[0].OutputLocation = "Database";
      vRptSet.ReportStyle[0].RptCriteriaSetID = "PrimaryCriteria";
      
      var paramRow = vRptSet.DynamicCriteriaParam.NewRow();
      
      vRptSet.DynamicCriteriaParam.Add(paramRow);
      
      string xmlCriteria = 
      "<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\"?>" +
      "<UICriteria>" +
          "<RptCriteriaSet>" +
              "<Company>WFEP1</Company>" +
              "<RptDefID>XSRV1102</RptDefID>" +
              "<RptCriteriaSetID>PrimaryCriteria</RptCriteriaSetID>" +
              "<Description>PrimaryCriteria</Description>" +
          "</RptCriteriaSet>" +
          "<RptCriteriaPrompt>" +
              "<PromptID>1</PromptID>" +
              "<PromptValue>" + combinedCerts + "</PromptValue>" +
              "<IsToken>false</IsToken>" +
              "<PromptName>SetCertNoList</PromptName>" +
              "<DataType>nvarchar</DataType>" +
              "<Label>SetCertNoList</Label>" +
          "</RptCriteriaPrompt>" +
          "<RptCriteriaPrompt>" +
              "<PromptID>2</PromptID>" +
              "<PromptValue>" + custID + "</PromptValue>" +
              "<IsToken>false</IsToken>" +
              "<PromptName>SetCustID</PromptName>" +
              "<DataType>nvarchar</DataType>" +
              "<Label>SetCustID</Label>" +
          "</RptCriteriaPrompt>" +
      "</UICriteria>";
      
      string sContext = callContextBpmData.ShortChar01;
      if(sContext == "Generate"){
        vRptSet.DynamicCriteriaParam[0].RowMod = "A";
        vRptSet.DynamicCriteriaParam[0].ReportStyleNum = 1001;
        vRptSet.DynamicCriteriaParam[0].SSRSRenderFormat = "PDF";
        vRptSet.DynamicCriteriaParam[0]["SetCertNoList"] = combinedCerts;
        vRptSet.DynamicCriteriaParam[0]["SetCustID"] = custID;
        vRptSet.DynamicCriteriaParam[0].UserCriteria = xmlCriteria;
        vRptSet.DynamicCriteriaParam[0].AutoAction = "SSRSPREVIEW"; // Atau SSRPreview
        vRptSet.DynamicCriteriaParam[0].WorkstationID = "web_"+callContextClient.CurrentUserId;
        
        vRptSvc.SubmitToAgent(vRptSet, "", 0, 0, "Erp.UIDynRpt.XSRV1102");
        
        sMessage = sMessage + "Generate Preview of LM Reminder Letter for Customer " + custID + Environment.NewLine;
        /*this.PublishInfoMessage(
            "Generate Preview of LM Reminder Letter for Customer " + custID,
            Ice.Common.BusinessObjectMessageType.Information,
            Ice.Bpm.InfoMessageDisplayMode.Individual,
            string.Empty,
            string.Empty
        );*/
      }
      else if(sContext == "Email"){
        vRptSet.DynamicCriteriaParam[0].RowMod = "A";
        vRptSet.DynamicCriteriaParam[0].ReportStyleNum = 1001;
        vRptSet.DynamicCriteriaParam[0].SSRSRenderFormat = "PDF";
        vRptSet.DynamicCriteriaParam[0]["SetCertNoList"] = combinedCerts;
        vRptSet.DynamicCriteriaParam[0]["SetCustID"] = custID;
        vRptSet.DynamicCriteriaParam[0].FaxSubject = "Reminder for LM/PM Renewal | " + getName;
        vRptSet.DynamicCriteriaParam[0].EMailTo = getEmail;
        vRptSet.DynamicCriteriaParam[0].EMailCC = "arga@opexcg.com;kenaka@opexcg.com";
        vRptSet.DynamicCriteriaParam[0].EMailBody = "Dear Sir/Madam,\n\n" +
        "Please find attached the reminder letter for the LM/PM renewal of your lifting equipment.\n" +
        "Kindly arrange for the necessary inspection and servicing ahead of the due date to avoid any delay in the renewal process.\n\n" +
        "Thank you.\n\n" +
        "Best regards,\n" +
        "Wong Fong";;
        vRptSet.DynamicCriteriaParam[0].UserCriteria = xmlCriteria;
        vRptSet.DynamicCriteriaParam[0].AutoAction = "SSRSPRINT"; // Atau SSRPreview
        vRptSet.DynamicCriteriaParam[0].WorkstationID = "web_"+callContextClient.CurrentUserId;
        
        vRptSvc.SubmitToAgent(vRptSet, "", 0, 0, "Erp.UIDynRpt.XSRV1102");
        sMessage = sMessage + "Send Email of LM Reminder Letter for Customer " + custID + Environment.NewLine;
        /*this.PublishInfoMessage(
            "Send Email of LM Reminder Letter for Customer " + custID,
            Ice.Common.BusinessObjectMessageType.Information,
            Ice.Bpm.InfoMessageDisplayMode.Individual,
            string.Empty,
            string.Empty
        );*/
      }
    }
    catch(Exception e){
      // 5. Publish an information message containing the customer number and their combined certificates
      this.PublishInfoMessage(
          e.Message,
          Ice.Common.BusinessObjectMessageType.Information,
          Ice.Bpm.InfoMessageDisplayMode.Individual,
          string.Empty,
          string.Empty
      );
    }
    
}
if(sMessage!=""){
  
  this.PublishInfoMessage(
      sMessage,
      Ice.Common.BusinessObjectMessageType.Information,
      Ice.Bpm.InfoMessageDisplayMode.Individual,
      string.Empty,
      string.Empty
  );
}