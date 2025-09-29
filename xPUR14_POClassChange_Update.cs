// 1. Ambil PO dan update ClassID
Erp.Tablesets.POTableset POSet = new Erp.Tablesets.POTableset();
Erp.Tablesets.POTableset EmptyPOSet = new Erp.Tablesets.POTableset();
Erp.Tablesets.POTableset GLCPOSet = new Erp.Tablesets.POTableset();
Erp.Contracts.POSvcContract POSvc = Ice.Assemblies.ServiceRenderer.GetService<Erp.Contracts.POSvcContract>(Db);

var vQuery = queryResultDataset.Results.FirstOrDefault();
POSet = POSvc.GetByID(vQuery.PODetail_PONUM);

var iPODetail = POSet.PODetail.FirstOrDefault(r=>r.POLine==vQuery.PODetail_POLine);
var iBasePODetailRow = Epicor.Data.BufferCopy.Copy(iPODetail);
var iUpdPODetailRow = Epicor.Data.BufferCopy.Copy(iPODetail);
EmptyPOSet.PODetail.Add(iBasePODetailRow);
EmptyPOSet.PODetail.Add(iUpdPODetailRow);
var iUpdPODetail = EmptyPOSet.PODetail[1];
iUpdPODetail.ClassID = vQuery.PODetail_ClassID;
iUpdPODetail.RowMod = "U";
POSvc.Update(ref EmptyPOSet);

// 2. Cari GL Account dari EntityGLC/PartClass
string company = iUpdPODetail.Company;
string classID = iUpdPODetail.ClassID;

// Cari EntityGLC -> GLCntrlAcct -> GLAccount
var glInfo = (from eg in Db.EntityGLC
              join gca in Db.GLCntrlAcct on new {
                  eg.Company,
                  eg.GLControlCode,
                  eg.GLControlType
              } equals new {
                  gca.Company,
                  gca.GLControlCode,
                  gca.GLControlType
              }
              join gla in Db.GLAccount on new {
                  gca.Company,
                  gca.GLAccount
              } equals new {
                  gla.Company,
                  GLAccount = gla.GLAccount1
              }
              where eg.Company == company
                 && eg.RelatedToFile == "PartClass"
                 && eg.Key1 == classID
                 && gca.GLAcctContext == "Inventory/Expense"
              select new {
                  gla.GLAccount1,
                  gla.SegValue1,
                  gla.SegValue2,
                  gla.SegValue3
              }).FirstOrDefault();

//POSvc.GetDefaultGLAccount(vQuery.PODetail_PONUM,vQuery.PODetail_POLine,1, ref DefaultPOSet);

// 1. Load PO with GetByID
// 2. Clear existing TGLC if needed
try {
  // 1. Clear jika perlu
GLCPOSet.PORelTGLC.Clear();

// 2. Add new row
GLCPOSet.PORelTGLC.Add(GLCPOSet.PORelTGLC.NewRow());

string sC = callContextClient.CurrentCompany;

var vDbGLBook = Db.GLBook.FirstOrDefault(r=>r.Company==sC);
var vDbCOA = Db.COA.FirstOrDefault(r=>r.Company==sC);
var vDbFiscalCal = Db.FiscalCal.FirstOrDefault(r=>r.Company==sC);

GLCPOSet.PORelTGLC[0].Company = sC;
GLCPOSet.PORelTGLC[0].RelatedToFile = "PORel";
GLCPOSet.PORelTGLC[0].Key1 = vQuery.PODetail_PONUM.ToString();
GLCPOSet.PORelTGLC[0].Key2 = vQuery.PODetail_POLine.ToString();
GLCPOSet.PORelTGLC[0].Key3 = "1";

GLCPOSet.PORelTGLC[0].GLAcctContext = "Expense";
GLCPOSet.PORelTGLC[0].BookID = vDbGLBook.BookID;
GLCPOSet.PORelTGLC[0].COACode = vDbCOA.COACode;
GLCPOSet.PORelTGLC[0].SysGLControlType = "PO Release";
GLCPOSet.PORelTGLC[0].SysGLControlCode = "142547982";
GLCPOSet.PORelTGLC[0].FiscalCalendarID = vDbFiscalCal.FiscalCalendarID;
GLCPOSet.PORelTGLC[0].CreateDate = DateTime.Today;
GLCPOSet.PORelTGLC[0].TranDate = DateTime.Today;
GLCPOSet.PORelTGLC[0].IsMainBook = true;
GLCPOSet.PORelTGLC[0].UserCanModify = false;

// Set nilai akunnya langsung
GLCPOSet.PORelTGLC[0].GLAccount = glInfo.GLAccount1;
GLCPOSet.PORelTGLC[0].SegValue1 = glInfo.SegValue1;
GLCPOSet.PORelTGLC[0].SegValue2 = glInfo.SegValue2;
GLCPOSet.PORelTGLC[0].SegValue3 = glInfo.SegValue3;

// Flag as insert
GLCPOSet.PORelTGLC[0].RowMod = "A";

// 3. Submit
POSvc.Update(ref GLCPOSet);


}
catch(Exception e){
  PublishInfoMessage(e.Message, Ice.Common.BusinessObjectMessageType.Information, Ice.Bpm.InfoMessageDisplayMode.Individual, "", "");
}
