Erp.Tablesets.JobEntryTableset sJobSet = new Erp.Tablesets.JobEntryTableset();
Erp.Tablesets.JobEntryTableset sUpdSet = new Erp.Tablesets.JobEntryTableset();
Erp.Tablesets.JobEntryTableset sAddSet = new Erp.Tablesets.JobEntryTableset();
Erp.Tablesets.JobEntryTableset sDelSet = new Erp.Tablesets.JobEntryTableset();
Erp.Contracts.JobEntrySvcContract sJobSvc = Ice.Assemblies.ServiceRenderer.GetService<Erp.Contracts.JobEntrySvcContract>(Db);

string sCompany = callContextClient.CurrentCompany;
string sJobNum = callContextBpmData.ShortChar01;

string sOutMsg = "";
string sN = Environment.NewLine;
bool sOutFlag = false;

sJobSet = sJobSvc.GetByID(sJobNum);

if (callContextBpmData.Checkbox01 == true)
{
    // === ADD NEW ===
    var sAddView = queryResultDataset.Results.Where(r => r.JobMtl_PartNum != "" && r.RowMod == "A");

    foreach (var a in sAddView)
    {
        ////Add for Custom AC-100 //No need, since this is already settled in GetNew method
        //sBillable = !a.FSCallDt_xWarrantyStatus_c && a.JobMtl_Billable;
        //End

        string iPartNum = a.JobMtl_PartNum;
        decimal iQtyPer = a.JobMtl_QtyPer;
        decimal iUnitPrice = a.JobMtl_DocUnitPrice;
        bool iBillable = a.JobMtl_Billable; //a.JobMtl_Billable;
        bool iBuyIt = a.JobMtl_BuyIt; //a.JobMtl_Billable;

        sJobSvc.GetNewJobMtl(ref sAddSet, sJobNum, 0);
        var sJobMtlAddRow = sAddSet.JobMtl[0];

        sJobMtlAddRow.PartNum = iPartNum;
        sJobSvc.ChangeJobMtlPartNum(ref sAddSet, true, ref iPartNum, Guid.NewGuid(), "", "", out sOutMsg, out sOutFlag, out sOutMsg, out sOutFlag, out sOutFlag, out sOutMsg);

        sJobMtlAddRow.Description = a.JobMtl_Description;
        sJobMtlAddRow.MfgComment = a.JobMtl_MfgComment;
        sJobMtlAddRow["xListCode_c"] = a.JobMtl_xListCode_c;
        sJobMtlAddRow["xNoCharge_c"] = a.JobMtl_xNoCharge_c;
        sJobMtlAddRow["xDiscountPercentage_c"] = a.Calculated_Discount;
        sJobMtlAddRow.dspBuyIt = a.JobMtl_BuyIt;
        sJobMtlAddRow.BuyIt = a.JobMtl_BuyIt;
        //sJobSvc.ChangeJobMtlBuyIt(ref sAddSet);
        sJobMtlAddRow.IUM = a.JobMtl_IUM;


        sJobSvc.Update(ref sAddSet);

        // Capture the new MtlSeq after insert
        int newMtlSeq = sJobMtlAddRow.MtlSeq;
        sAddSet.JobMtl.Clear();

        // === Safe Update (Qty, Billable, Price)
        Action refreshAndUpdate = () =>
        {
            var jobSetRefresh = sJobSvc.GetByID(sJobNum);
            var mtlRow = jobSetRefresh.JobMtl.FirstOrDefault(m => m.MtlSeq == newMtlSeq);
            if (mtlRow == null) return;

            // === QtyPer
            if (iQtyPer != mtlRow.QtyPer)
            {
                var baseRow = Epicor.Data.BufferCopy.Copy(mtlRow);
                var updRow = Epicor.Data.BufferCopy.Copy(mtlRow);
                updRow.RowMod = "U";
                updRow.QtyPer = iQtyPer;

                sUpdSet.JobMtl.Add(baseRow);
                sUpdSet.JobMtl.Add(updRow);

                sJobSvc.ChangeJobMtlQtyPer(ref sUpdSet);
                sJobSvc.ChangeJobMtlEstSplitCosts(ref sUpdSet);
                sJobSvc.Update(ref sUpdSet);
                sUpdSet.JobMtl.Clear();
            }

            // === Billable
            var mtlAfterQty = sJobSvc.GetByID(sJobNum).JobMtl.FirstOrDefault(m => m.MtlSeq == newMtlSeq);
            if (mtlAfterQty != null && iBillable != mtlAfterQty.Billable)
            {
                var baseRow = Epicor.Data.BufferCopy.Copy(mtlAfterQty);
                var updRow = Epicor.Data.BufferCopy.Copy(mtlAfterQty);
                updRow.RowMod = "U";
                updRow.Billable = iBillable;

                sUpdSet.JobMtl.Add(baseRow);
                sUpdSet.JobMtl.Add(updRow);

                sJobSvc.ChangeJobMtlBillable(ref sUpdSet);
                sJobSvc.Update(ref sUpdSet);
                sUpdSet.JobMtl.Clear();
            }

            // === Unit Price
            var mtlFinal = sJobSvc.GetByID(sJobNum).JobMtl.FirstOrDefault(m => m.MtlSeq == newMtlSeq);
            if (mtlFinal != null && iUnitPrice != mtlFinal.DocUnitPrice)
            {
                var baseRow = Epicor.Data.BufferCopy.Copy(mtlFinal);
                var updRow = Epicor.Data.BufferCopy.Copy(mtlFinal);
                updRow.RowMod = "U";

                updRow.DocDisplayUnitPrice = iUnitPrice;
                updRow.DisplayUnitPrice = iUnitPrice;
                updRow.DocBillableUnitPrice = iUnitPrice;
                updRow.BillableUnitPrice = iUnitPrice;
                updRow.DocUnitPrice = iUnitPrice;
                updRow.UnitPrice = iUnitPrice;

                sUpdSet.JobMtl.Add(baseRow);
                sUpdSet.JobMtl.Add(updRow);

                sJobSvc.ChangeJobMtlDisplayUnitPrice(ref sUpdSet);
                sJobSvc.Update(ref sUpdSet);
                sUpdSet.JobMtl.Clear();
            }
        };

        refreshAndUpdate();
    }


    // === UPDATE EXISTING ===
    // === UPDATE EXISTING ===
    var sUpdView = queryResultDataset.Results.Where(r => r.JobMtl_PartNum != "" && r.RowMod == "U");

    foreach (var a in sUpdView)
    {
        int iMtlSeq = a.JobMtl_MtlSeq;
        string iPartNum = a.JobMtl_PartNum;
        decimal iQtyPer = a.JobMtl_QtyPer;
        decimal iUnitPrice = a.JobMtl_DocUnitPrice;
        bool iBillable = a.JobMtl_Billable; //a.JobMtl_Billable;

        // Ambil baris latest dari DB pakai MtlSeq
        var latest = sJobSvc.GetByID(sJobNum).JobMtl.FirstOrDefault(m => m.MtlSeq == iMtlSeq);
        if (latest == null) continue;


        // === PartNum Change
        if (iPartNum != latest.PartNum)
        {
            try
            {
                var baseRow = Epicor.Data.BufferCopy.Copy(latest);
                var updRow = Epicor.Data.BufferCopy.Copy(latest);
                updRow.RowMod = "U";
                updRow.PartNum = iPartNum;

                sUpdSet.JobMtl.Add(baseRow);
                sUpdSet.JobMtl.Add(updRow);

                sJobSvc.ChangeJobMtlPartNum(ref sUpdSet, false, ref iPartNum, Guid.NewGuid(), "", "", out sOutMsg, out sOutFlag, out sOutMsg, out sOutFlag, out sOutFlag, out sOutMsg);
                sJobSvc.Update(ref sUpdSet);
                sUpdSet.JobMtl.Clear();
            }
            catch (Exception e)
            {
                PublishInfoMessage("Update PartNum Failed: " + e.Message, Ice.Common.BusinessObjectMessageType.Error, Ice.Bpm.InfoMessageDisplayMode.Individual, "", "");
            }
        }

        //=== Description, MfgComment, xListCode_c, xNoCharge_c, Billable
        try
        {
            var refreshed = sJobSvc.GetByID(sJobNum).JobMtl.FirstOrDefault(m => m.MtlSeq == iMtlSeq);
            if (refreshed != null)
            {
                var baseRow = Epicor.Data.BufferCopy.Copy(refreshed);
                var updRow = Epicor.Data.BufferCopy.Copy(refreshed);
                updRow.RowMod = "U";
                updRow.IUM = a.JobMtl_IUM;
                updRow.Description = a.JobMtl_Description;
                updRow.MfgComment = a.JobMtl_MfgComment;
                updRow["xListCode_c"] = a.JobMtl_xListCode_c;
                updRow["xNoCharge_c"] = a.JobMtl_xNoCharge_c;
                updRow["xDiscountPercentage_c"] = a.Calculated_Discount;
                updRow.Billable = iBillable;
                baseRow.dspBuyIt = a.JobMtl_BuyIt;
                updRow.dspBuyIt = a.JobMtl_BuyIt;
                baseRow.BuyIt = a.JobMtl_BuyIt;
                updRow.BuyIt = a.JobMtl_BuyIt;
                //sJobSvc.CheckJobMtlBuyIt(ref sUpdSet);
                //sJobSvc.ChangeJobMtlBuyIt(ref sUpdSet);

                sUpdSet.JobMtl.Add(baseRow);
                sUpdSet.JobMtl.Add(updRow);

                sJobSvc.Update(ref sUpdSet);
                sUpdSet.JobMtl.Clear();
                PublishInfoMessage("Update Ref " + iBillable.ToString(), Ice.Common.BusinessObjectMessageType.Information, Ice.Bpm.InfoMessageDisplayMode.Individual, "", "");

            }
        }
        catch (Exception e)
        {
            PublishInfoMessage("Update Billable Failed: " + e.Message, Ice.Common.BusinessObjectMessageType.Error, Ice.Bpm.InfoMessageDisplayMode.Individual, "", "");
        }

        // === QtyPer
        try
        {
            var refreshed = sJobSvc.GetByID(sJobNum).JobMtl.FirstOrDefault(m => m.MtlSeq == iMtlSeq);
            if (refreshed != null && iQtyPer != refreshed.QtyPer)
            {
                var baseRow = Epicor.Data.BufferCopy.Copy(refreshed);
                var updRow = Epicor.Data.BufferCopy.Copy(refreshed);
                updRow.RowMod = "U";
                updRow.QtyPer = iQtyPer;

                sUpdSet.JobMtl.Add(baseRow);
                sUpdSet.JobMtl.Add(updRow);

                sJobSvc.ChangeJobMtlQtyPer(ref sUpdSet);
                sJobSvc.ChangeJobMtlEstSplitCosts(ref sUpdSet);
                sJobSvc.Update(ref sUpdSet);
                sUpdSet.JobMtl.Clear();
            }
        }
        catch (Exception e)
        {
            PublishInfoMessage("Update Qty Failed: " + e.Message, Ice.Common.BusinessObjectMessageType.Error, Ice.Bpm.InfoMessageDisplayMode.Individual, "", "");
        }

        // === UnitPrice
        try
        {
            var refreshed = sJobSvc.GetByID(sJobNum).JobMtl.FirstOrDefault(m => m.MtlSeq == iMtlSeq);
            if (refreshed != null && iUnitPrice != refreshed.DocUnitPrice)
            {
                var baseRow = Epicor.Data.BufferCopy.Copy(refreshed);
                var updRow = Epicor.Data.BufferCopy.Copy(refreshed);
                updRow.RowMod = "U";

                updRow.DocDisplayUnitPrice = iUnitPrice;
                updRow.DisplayUnitPrice = iUnitPrice;
                updRow.DocBillableUnitPrice = iUnitPrice;
                updRow.BillableUnitPrice = iUnitPrice;
                updRow.DocUnitPrice = iUnitPrice;
                updRow.UnitPrice = iUnitPrice;

                sUpdSet.JobMtl.Add(baseRow);
                sUpdSet.JobMtl.Add(updRow);

                sJobSvc.ChangeJobMtlDisplayUnitPrice(ref sUpdSet);
                sJobSvc.Update(ref sUpdSet);
                sUpdSet.JobMtl.Clear();
            }
        }
        catch (Exception e)
        {
            PublishInfoMessage("Update Price Failed: " + e.Message, Ice.Common.BusinessObjectMessageType.Error, Ice.Bpm.InfoMessageDisplayMode.Individual, "", "");
        }
    }

}
else if (callContextBpmData.Checkbox02 == true)
{
    // === MULTIPLE DELETE SECTION ===
    var sDelView = queryResultDataset.Results.Where(r => r.JobMtl_PartNum != "" && r.RowMod == "U" && r.Calculated_Select == true);
    foreach (var a in sDelView)
    {
        Guid iGuid = a.JobMtl_SysRowID;
        var mtlRows = sJobSet.JobMtl.Where(m => m.SysRowID == iGuid);
        foreach (var s in mtlRows)
        {
            var baseRow = Epicor.Data.BufferCopy.Copy(s);
            var delRow = Epicor.Data.BufferCopy.Copy(s);
            delRow.RowMod = "D";

            sDelSet.JobMtl.Add(baseRow);
            sDelSet.JobMtl.Add(delRow);
        }
    }

    if (sDelSet.JobMtl.Count > 0)
    {
        try
        {
            sJobSvc.Update(ref sDelSet);
            sDelSet.JobMtl.Clear();
        }
        catch (Exception e)
        {
            PublishInfoMessage("Delete Failed: " + e.Message, Ice.Common.BusinessObjectMessageType.Error, Ice.Bpm.InfoMessageDisplayMode.Individual, "", "");
        }
    }
}
else if (callContextBpmData.Checkbox03 == true)
{
    string sMsg = "";
    string sD = "----------------------------------------";
    int sSuccess = 0;
    int sFailed = 0;

    Erp.Tablesets.IssueReturnTableset IssueSet = new Erp.Tablesets.IssueReturnTableset();
    Erp.Contracts.IssueReturnSvcContract IssueSvc = Ice.Assemblies.ServiceRenderer.GetService<Erp.Contracts.IssueReturnSvcContract>(Db);

    // Group logic to avoid duplicate per part, and choose best row
    var groupedIssues = queryResultDataset.Results
        .Where(r => r.Calculated_CurrentIssue != 0 && r.Calculated_Select == true)
        .GroupBy(r => r.JobMtl_MtlSeq)
        .Select(g =>
        {
            // Prefer 'U' rowmod
            var updateRow = g.FirstOrDefault(r => r.RowMod == "U");
            if (updateRow != null)
                return updateRow;

            // Otherwise, fallback to "" (new record)
            return g.FirstOrDefault(r => string.IsNullOrEmpty(r.RowMod));
        })
        .Where(r => r != null)
        .OrderBy(r => r.JobMtl_MtlSeq)
        .ToList();

    foreach (var d in groupedIssues)
    {
        string iPartNum = d.JobMtl_PartNum;
        int iMtlSeq = d.JobMtl_MtlSeq;
        string iWarehouseCode = d.Calculated_WarehouseIssue;
        string iBinNum = d.Calculated_BinIssue;
        string iLotNum = d.Calculated_LotIssue;
        decimal iCurrentIssue = d.Calculated_CurrentIssue;

        try
        {
            // New IssueReturn row
            IssueSvc.GetNewIssueReturnToJob(sJobNum, 0, "STK-MTL", Guid.NewGuid(), out sOutMsg, ref IssueSet);
            var irRow = IssueSet.IssueReturn[0];

            // Populate fields
            irRow.Company = sCompany;
            irRow.PartNum = iPartNum;
            irRow.FromJobNum = sJobNum;
            irRow.FromAssemblySeq = 0;
            irRow.FromJobSeq = iMtlSeq;
            irRow.ToJobNum = sJobNum;
            irRow.ToAssemblySeq = 0;
            irRow.ToJobSeq = iMtlSeq;
            irRow.ToWarehouseCode = iWarehouseCode;
            irRow.LotNum = iLotNum;
            irRow.RowMod = "U";
            irRow.FromWarehouseCode = iWarehouseCode;
            irRow.FromBinNum = iBinNum;
            //irRow.ToBinNum = iBinNum;

            // Triggers & validation
            IssueSvc.OnChangingToJobSeq(iMtlSeq, ref IssueSet);
            IssueSvc.OnChangeToJobSeq(ref IssueSet, "IssueMaterial", out sOutMsg);
            IssueSvc.OnChangeTranQty(iCurrentIssue, ref IssueSet);

            IssueSvc.OnChangeFromWarehouse(ref IssueSet, iWarehouseCode);
            irRow.FromWarehouseCode = iWarehouseCode;
            IssueSvc.OnChangeFromBinNum(ref IssueSet);
            irRow.FromBinNum = iBinNum;
            IssueSvc.PrePerformMaterialMovement(ref IssueSet, out sOutFlag);

            // Perform Issue
            string msg1, msg2;
            irRow.FromBinNum = iBinNum;
            IssueSvc.PerformMaterialMovement(false, ref IssueSet, out msg1, out msg2);

            // Log success
            sMsg += $"Material Seq {iMtlSeq} Part {iPartNum} Successfully Issued from Warehouse {iWarehouseCode} Bin {iBinNum} by {iCurrentIssue}{sN}";
            sSuccess++;

            // Cleanup
            IssueSet.IssueReturn.Clear();
        }
        catch (Exception ex)
        {
            sMsg += $"Material Seq {iMtlSeq} Failed to Issue - Reason: {ex.Message}{sN}";
            sFailed++;
        }
    }

    sMsg += sD + sN + $"Total Success: {sSuccess}, Total Failed: {sFailed}";
    PublishInfoMessage(sMsg, Ice.Common.BusinessObjectMessageType.Information, Ice.Bpm.InfoMessageDisplayMode.Individual, "", "");

    //Test Add Comment
}

