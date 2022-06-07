Console.WriteLine("<採購單取得商品報價>");

Main();

/// <summary>
/// 功能目的: 
///		採購單需要依照出貨的日期取得商品的報價，而商品報價有分一般報價和特殊報價。
///		一般報價通常為全年度常態性的報價。特殊報價通常價格較低且有期間限定，且特殊報價需先使用，否則有違約的可能。
/// 功能說明:
///		1.	先準備好採購資料集合、報價單資料集合
///		2.	依照下方的邏輯去篩選
///		3.	主要透過採購資料集合的 Done 屬性，來判斷至否結束當前的遞迴。
/// </summary>
static void Main()
{
	var purchaseList = new List<PurchaseModel>();
	//	商品機型(1)、出貨日期(2022/06/05)、採購數量(100)
	purchaseList.Add(new PurchaseModel() { Id = 1, ProdNo = 1, PurchaseQty = 100, ShippingDate = new DateTime(2022, 06, 20), Done = false });
	//	商品機型(2)、出貨日期(2022/06/05)、採購數量(100)
	purchaseList.Add(new PurchaseModel() { Id = 2, ProdNo = 2, PurchaseQty = 50, ShippingDate = new DateTime(2022, 06, 15), Done = false });
	//	商品機型(3)、出貨日期(2022/06/05)、採購數量(100)
	purchaseList.Add(new PurchaseModel() { Id = 3, ProdNo = 3, PurchaseQty = 60, ShippingDate = new DateTime(2022, 06, 20), Done = false });


	var quotationList = new List<QuotationModel>();
	//	一般報價單(1)、商品機型(1)、生效範圍(2022/06/01~2022/12/31)、價格(80)、剩餘數量(20)
	quotationList.Add(new QuotationModel() { Id = 1, QuotationType = 1, ProdNo = 1, RemainQty = 20, Price = 80, StartDate = new DateTime(2022, 06, 01), EndDate = new DateTime(2022, 12, 31) });
	//	一般報價單(1)、商品機型(2)、生效範圍(2022/06/01~2022/12/31)、價格(50)、剩餘數量(20)
	quotationList.Add(new QuotationModel() { Id = 2, QuotationType = 1, ProdNo = 2, RemainQty = 20, Price = 50, StartDate = new DateTime(2022, 06, 01), EndDate = new DateTime(2022, 12, 31) });
	//	特殊報價單(2)、商品機型(1)、生效範圍(2022/06/01~2022/6/30)、價格(60)、剩餘數量(20)
	quotationList.Add(new QuotationModel() { Id = 3, QuotationType = 2, ProdNo = 1, RemainQty = 20, Price = 60, StartDate = new DateTime(2022, 06, 01), EndDate = new DateTime(2022, 06, 30) });
	//	特殊報價單(2)、商品機型(1)、生效範圍(2022/06/15~2022/7/31)、價格(40)、剩餘數量(20)
	quotationList.Add(new QuotationModel() { Id = 4, QuotationType = 2, ProdNo = 1, RemainQty = 20, Price = 40, StartDate = new DateTime(2022, 06, 15), EndDate = new DateTime(2022, 07, 31) });

	var data = Recursive(purchaseList, quotationList);
}

static IEnumerable<PurchaseWithPriceModel> Recursive(List<PurchaseModel> purchaseList, List<QuotationModel> quotationList)
{
	//  宣告回傳的資料
	List<PurchaseWithPriceModel> outter = new List<PurchaseWithPriceModel>();

	//  遍瀝採購申請單明細檔，且 "篩選出" 採購數量需大於0，且未轉完的資料
	foreach (var t in purchaseList.Where(item => item.PurchaseQty > 0 && item.Done == false))
	{
		//	篩選出:
		//		a) 數量大於0
		//		b) 機型相同
		//		c) 出貨日期在起迄範圍中
		//		d) 特殊報價單排在前面
		//		e) 迄日最接近的排在最前面
		//	的一般報價單和特殊報價單的對應集合
		var sourceItem = quotationList
			.Where(p =>
				p.RemainQty > 0 &&  // a) 剩餘數量大於0
				p.ProdNo == t.ProdNo &&     // b) 機型相同
				p.StartDate <= t.ShippingDate && p.EndDate >= t.ShippingDate)    // c) 出貨日期在起迄範圍中)
			.OrderByDescending(p => p.QuotationType)    //	d) 特殊報價單排在前面
			.ThenBy(p => p.EndDate)     //	e) 迄日最接近的排在最前面
			.FirstOrDefault();

        if (sourceItem != null)
        {
			// 當前使用數量
			int? subtraction = 0;

			//  如果是一般報價，庫存無上限，所以不扣庫存
			if (sourceItem.QuotationType == 1)
			{
				subtraction = t.PurchaseQty;     //  當前使用數量 = 採購數量

				t.PurchaseQty -= subtraction;    //  採購數量 扣除 當前使用數量
			}
			//  如果是特殊報價，庫存有上限，需扣庫存
			else if (sourceItem.QuotationType == 2)
			{
				//  紀錄當前使用數量 = 採購數量 和 庫存剩餘數量的交叉併補
				subtraction = t.PurchaseQty - sourceItem.RemainQty >= 0 ? sourceItem.RemainQty : t.PurchaseQty;

				t.PurchaseQty -= subtraction;    //  採購數量 扣除 當前使用數量

				sourceItem.RemainQty -= subtraction;   //  原始剩餘數量 扣除 當前使用數量
			}

			//  採購數量若為0，表示庫存都已轉為價格明細，之後就換下一筆資料
			t.Done = t.PurchaseQty == 0 ? true : false;

			//  加入回傳的資料集合
			outter.Add(new PurchaseWithPriceModel { QuotationId = sourceItem.Id, ProdNo = sourceItem.ProdNo, PurchaseQty = subtraction, Price = sourceItem.Price, QuotationType = sourceItem.QuotationType, StartDate = sourceItem.StartDate, EndDate = sourceItem.EndDate, ShippingDate = t.ShippingDate });

			//  遞迴並聯集回傳的資料
			outter = outter.Union(Recursive(purchaseList, quotationList)).ToList();
		}                
		//  若 一般報價單 和 特殊報價單 的對應集合是 null，表示該商品沒有庫存可以使用
		else
		{
			//  因無庫存可使用，停止轉換
			t.Done = true;

			//  加入回傳的資料集合，但是採購量轉負數，表示尚有不足的庫存
			outter.Add(new PurchaseWithPriceModel { ProdNo = t.ProdNo, PurchaseQty = -t.PurchaseQty, QuotationId = null, Price = null, ShippingDate = t.ShippingDate });
		}
	}

	return outter;
}


/// <summary>
/// 一般報價單和特殊報價單集合類型
/// </summary>
public class QuotationModel
{
	/// <summary>
	/// PK
	/// </summary>
	public int? Id { get; set; }

	/// <summary>
	/// 報價單種類。
	///		1: 一般報價單(Normal)
	///		2: 特殊報價單(Special)
	/// </summary>
	public int QuotationType { get; set; }

	/// <summary>
	/// 商品型號
	/// </summary>
	public int ProdNo { get; set; }

	/// <summary>
	/// 剩餘數量
	/// </summary>
	public int? RemainQty { get; set; }

	/// <summary>
	/// 價格
	/// </summary>
	public int? Price { get; set; }

	/// <summary>
	/// 生效起始日期
	/// </summary>
	public DateTime StartDate { get; set; }

	/// <summary>
	/// 生效結束日期
	/// </summary>
	public DateTime EndDate { get; set; }
}

/// <summary>
/// 採購單
/// </summary>
public class PurchaseModel
{
	/// <summary>
	/// PK
	/// </summary>
	public int Id { get; set; }

	/// <summary>
	/// 商品型號
	/// </summary>
	public int ProdNo { get; set; }

	/// <summary>
	/// 採購數量
	/// </summary>
	public int? PurchaseQty { get; set; }

	/// <summary>
	/// 出貨日
	/// </summary>
	public DateTime ShippingDate { get; set; }

	/// <summary>
	/// 是否全部數量都已取得報價
	/// </summary>
	public bool Done { get; set; } = false;
}

/// <summary>
/// 採購單取得報價單
/// </summary>
public class PurchaseWithPriceModel
{
	/// <summary>
	/// PK
	/// </summary>
	public int? QuotationId { get; set; }

	/// <summary>
	/// 報價單種類。
	///		1: 一般報價單(Normal)
	///		2: 特殊報價單(Special)
	/// </summary>
	public int QuotationType { get; set; }

	/// <summary>
	/// 商品型號
	/// </summary>
	public int ProdNo { get; set; }

	/// <summary>
	/// 採購數量
	/// </summary>
	public int? PurchaseQty { get; set; }

	/// <summary>
	/// 價格
	/// </summary>
	public int? Price { get; set; }

	/// <summary>
	/// 出貨日
	/// </summary>
	public DateTime ShippingDate { get; set; }

	/// <summary>
	/// 生效起始日期
	/// </summary>
	public DateTime StartDate { get; set; }

	/// <summary>
	/// 生效結束日期
	/// </summary>
	public DateTime EndDate { get; set; }
}