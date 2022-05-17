using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.IO;
using System.Runtime.InteropServices;

using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.ADF;
using ESRI.ArcGIS.SystemUI;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.Display;

namespace ArcgisApp
{
    public sealed partial class MainForm : Form
    {
        #region class private members
        private IMapControl3 m_mapControl = null;
        private string m_mapDocumentName = string.Empty;
        #endregion

        #region class constructor
        public MainForm()
        {
            InitializeComponent();
        }
        #endregion


        IFeatureLayer pointLayer = null;
        IFeatureLayer lineLayer = null;
        private void MainForm_Load(object sender, EventArgs e)
        {
            //get the MapControl
            m_mapControl = (IMapControl3)axMapControl1.Object;
            string mxdFile = System.IO.Path.Combine(Application.StartupPath, "ChinaMap\\map.mxd");
            axMapControl1.LoadMxFile(mxdFile);

            pointLayer = CreateFeatureLayerInmemeory("PLayer", "点图层", axMapControl1.Map.SpatialReference, esriGeometryType.esriGeometryPoint, null);
            lineLayer = CreateFeatureLayerInmemeory("LLayer", "线图层", axMapControl1.Map.SpatialReference, esriGeometryType.esriGeometryPolyline, null);

            axMapControl1.AddLayer(pointLayer);
            axMapControl1.AddLayer(lineLayer);
        }

        /// <summary>
        /// 在内存中创建图层
        /// </summary>
        /// <param name="DataSetName">数据集名称</param>
        /// <param name="AliaseName">别名</param>
        /// <param name="SpatialRef">空间参考</param>
        /// <param name="GeometryType">几何类型</param>
        /// <param name="PropertyFields">属性字段集合</param>
        /// <returns>IfeatureLayer</returns>
        public static IFeatureLayer CreateFeatureLayerInmemeory(string DataSetName, string AliaseName, ISpatialReference SpatialRef, esriGeometryType GeometryType, IFields PropertyFields)
        {
            IWorkspaceFactory workspaceFactory = new InMemoryWorkspaceFactoryClass();
            ESRI.ArcGIS.Geodatabase.IWorkspaceName workspaceName = workspaceFactory.Create("", "MyWorkspace", null, 0);
            ESRI.ArcGIS.esriSystem.IName name = (IName)workspaceName;
            ESRI.ArcGIS.Geodatabase.IWorkspace inmemWor = (IWorkspace)name.Open();
            IField oField = new FieldClass();
            IFields oFields = new FieldsClass();
            IFieldsEdit oFieldsEdit = null;
            IFieldEdit oFieldEdit = null;
            IFeatureClass oFeatureClass = null;
            IFeatureLayer oFeatureLayer = null;
            try
            {
                oFieldsEdit = oFields as IFieldsEdit;
                oFieldEdit = oField as IFieldEdit;
                if (PropertyFields != null && PropertyFields.FieldCount > 0)
                {
                    for (int i = 0; i < PropertyFields.FieldCount; i++)
                    {
                        oFieldsEdit.AddField(PropertyFields.get_Field(i));
                    }
                }
                IGeometryDef geometryDef = new GeometryDefClass();
                IGeometryDefEdit geometryDefEdit = (IGeometryDefEdit)geometryDef;
                geometryDefEdit.AvgNumPoints_2 = 5;
                geometryDefEdit.GeometryType_2 = GeometryType;
                geometryDefEdit.GridCount_2 = 1;
                geometryDefEdit.HasM_2 = false;
                geometryDefEdit.HasZ_2 = false;
                geometryDefEdit.SpatialReference_2 = SpatialRef;
                oFieldEdit.Name_2 = "SHAPE";
                oFieldEdit.Type_2 = esriFieldType.esriFieldTypeGeometry;
                oFieldEdit.GeometryDef_2 = geometryDef;
                oFieldEdit.IsNullable_2 = true;
                oFieldEdit.Required_2 = true;
                oFieldsEdit.AddField(oField);
                oFeatureClass = (inmemWor as IFeatureWorkspace).CreateFeatureClass(DataSetName, oFields, null, null, esriFeatureType.esriFTSimple, "SHAPE", "");
                (oFeatureClass as IDataset).BrowseName = DataSetName;
                oFeatureLayer = new FeatureLayerClass();
                oFeatureLayer.Name = AliaseName;
                oFeatureLayer.FeatureClass = oFeatureClass;

                ISimpleRenderer pSimpleRenderer = new SimpleRendererClass();
                switch (GeometryType)
                {
                    case esriGeometryType.esriGeometryPoint:
                        pSimpleRenderer.Symbol = GetPointStyle() as ISymbol;
                        break;
                    case esriGeometryType.esriGeometryPolyline:
                        pSimpleRenderer.Symbol = GetLineStyle() as ISymbol;
                        break;
                }
                IGeoFeatureLayer m_pGeoFeatureLayer = oFeatureLayer as IGeoFeatureLayer;
                m_pGeoFeatureLayer.Renderer = pSimpleRenderer as IFeatureRenderer;
            }
            catch
            {
            }
            finally
            {
                try
                {
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(oField);
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(oFields);
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(oFieldsEdit);
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(oFieldEdit);
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(name);
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(workspaceFactory);
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(workspaceName);
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(inmemWor);
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(oFeatureClass);
                }
                catch { }

                GC.Collect();
            }
            return oFeatureLayer;
        }

        #region Main Menu event handlers
        private void menuNewDoc_Click(object sender, EventArgs e)
        {
            //execute New Document command
            ICommand command = new CreateNewDocument();
            command.OnCreate(m_mapControl.Object);
            command.OnClick();
        }

        private void menuOpenDoc_Click(object sender, EventArgs e)
        {
            //execute Open Document command
            ICommand command = new ControlsOpenDocCommandClass();
            command.OnCreate(m_mapControl.Object);
            command.OnClick();
        }

        private void menuSaveDoc_Click(object sender, EventArgs e)
        {
            //execute Save Document command
            if (m_mapControl.CheckMxFile(m_mapDocumentName))
            {
                //create a new instance of a MapDocument
                IMapDocument mapDoc = new MapDocumentClass();
                mapDoc.Open(m_mapDocumentName, string.Empty);

                //Make sure that the MapDocument is not readonly
                if (mapDoc.get_IsReadOnly(m_mapDocumentName))
                {
                    MessageBox.Show("Map document is read only!");
                    mapDoc.Close();
                    return;
                }

                //Replace its contents with the current map
                mapDoc.ReplaceContents((IMxdContents)m_mapControl.Map);

                //save the MapDocument in order to persist it
                mapDoc.Save(mapDoc.UsesRelativePaths, false);

                //close the MapDocument
                mapDoc.Close();
            }
        }

        private void menuSaveAs_Click(object sender, EventArgs e)
        {
            //execute SaveAs Document command
            ICommand command = new ControlsSaveAsDocCommandClass();
            command.OnCreate(m_mapControl.Object);
            command.OnClick();
        }

        private void menuExitApp_Click(object sender, EventArgs e)
        {
            //exit the application
            Application.Exit();
        }
        #endregion

        //listen to MapReplaced evant in order to update the statusbar and the Save menu
        private void axMapControl1_OnMapReplaced(object sender, IMapControlEvents2_OnMapReplacedEvent e)
        {
            //get the current document name from the MapControl
            m_mapDocumentName = m_mapControl.DocumentFilename;
        }

        private void axMapControl1_OnMouseMove(object sender, IMapControlEvents2_OnMouseMoveEvent e)
        {
            statusBarXY.Text = string.Format("{0}, {1}  {2}", e.mapX.ToString("#######.##"), e.mapY.ToString("#######.##"), axMapControl1.MapUnits.ToString().Substring(4));
        }

        private void btAdd_Click(object sender, EventArgs e)
        {
            double x1 = 0;
            try
            {
                x1 = Convert.ToDouble(tbX1.Text);
                if (x1 > 180 || x1 < -180)
                {
                    MessageBox.Show("X1必须在180~-180");
                    return;
                }
            }
            catch
            {
                MessageBox.Show("X1必须是小数;");
                return;
            }


            double y1 = 0;
            try
            {
                y1 = Convert.ToDouble(tbY1.Text);
                if (y1 > 90 || y1 < -90)
                {
                    MessageBox.Show("Y1必须在90~-90之间");
                    return;
                }
            }
            catch
            {
                MessageBox.Show("Y1必须是小数;");
                return;
            }

            double x2 = 0;
            try
            {
                x2 = Convert.ToDouble(tbX2.Text);
                if (x2 > 180 || x2 < -180)
                {
                    MessageBox.Show("X2必须在180~-180");
                    return;
                }
            }
            catch
            {
                MessageBox.Show("X2必须是小数;");
                return;
            }

            double y2 = 0;
            try
            {
                y2 = Convert.ToDouble(tbY2.Text);
                if (y2 > 90 || y2 < -90)
                {
                    MessageBox.Show("Y2必须在90~-90之间");
                    return;
                }
            }
            catch
            {
                MessageBox.Show("Y2必须是小数;");
                return;
            }


            AddPoint(x1, y1);
            AddPoint(x2, y2);
            AddLine(x1, y1, x2, y2);

            this.axMapControl1.Refresh();//刷新地图 

        }

        private void AddPoint(double x, double y)
        {
            IFeatureClass pFeatCls = pointLayer.FeatureClass;//定义一个要素集合，并获取图层的要素集合  
            IFeatureClassWrite fr = (IFeatureClassWrite)pFeatCls;//定义一个实现新增要素的接口实例，并该实例作用于当前图层的要素集  
            IWorkspaceEdit w = (pFeatCls as IDataset).Workspace as IWorkspaceEdit;//定义一个工作编辑工作空间，用于开启前图层的编辑状态  
            IFeature f;//定义一个IFeature实例，用于添加到当前图层上  
            w.StartEditing(true);//开启编辑状态  
            w.StartEditOperation();//开启编辑操作  
            IPoint p;//定义一个点，用来作为IFeature实例的形状属性，即shape属性  
            //下面是设置点的坐标和参考系  
            p = new PointClass();
            p.SpatialReference = this.axMapControl1.SpatialReference;
            p.X = x;
            p.Y = y;

            //将IPoint设置为IFeature的shape属性时，需要通过中间接口IGeometry转换  
            IGeometry peo;
            peo = p;
            f = pFeatCls.CreateFeature();//实例化IFeature对象， 这样IFeature对象就具有当前图层上要素的字段信息  
            f.Shape = peo;//设置IFeature对象的形状属性  
            
            //f.set_Value(3, "house1");//设置IFeature对象的索引是3的字段值  
            f.Store();//保存IFeature对象  
            fr.WriteFeature(f);//将IFeature对象，添加到当前图层上  
            w.StopEditOperation();//停止编辑操作  
            w.StopEditing(true);//关闭编辑状态，并保存修改  
            //this.axMapControl1.Refresh();//刷新地图  
        }

        private void AddLine(double x1, double y1, double x2, double y2)
        {
            IFeatureClass pFeatCls = lineLayer.FeatureClass;//定义一个要素集合，并获取图层的要素集合  
            IFeatureClassWrite fr = (IFeatureClassWrite)pFeatCls;//定义一个实现新增要素的接口实例，并该实例作用于当前图层的要素集  
            IWorkspaceEdit w = (pFeatCls as IDataset).Workspace as IWorkspaceEdit;//定义一个工作编辑工作空间，用于开启前图层的编辑状态  
            IFeature f;//定义一个IFeature实例，用于添加到当前图层上  
            w.StartEditing(true);//开启编辑状态  
            w.StartEditOperation();//开启编辑操作  
            IPoint p1;//定义一个点，用来作为IFeature实例的形状属性，即shape属性  
            //下面是设置点的坐标和参考系  
            p1 = new PointClass();
            p1.SpatialReference = this.axMapControl1.SpatialReference;
            p1.X = x1;
            p1.Y = y1;

            IPoint p2;//定义一个点，用来作为IFeature实例的形状属性，即shape属性  
            //下面是设置点的坐标和参考系  
            p2 = new PointClass();
            p2.SpatialReference = this.axMapControl1.SpatialReference;
            p2.X = x2;
            p2.Y = y2;

            IPointCollection m_PointCollection = new PolylineClass();
            m_PointCollection.AddPoint(p1);
            m_PointCollection.AddPoint(p2);

            IPolyline m_Polyline = new PolylineClass();
            
            m_Polyline = m_PointCollection as IPolyline;

            //将IPoint设置为IFeature的shape属性时，需要通过中间接口IGeometry转换  
            IGeometry peo;
            peo = m_Polyline;
            f = pFeatCls.CreateFeature();//实例化IFeature对象， 这样IFeature对象就具有当前图层上要素的字段信息  
            f.Shape = peo;//设置IFeature对象的形状属性  
            //f.set_Value(3, "house1");//设置IFeature对象的索引是3的字段值  
            f.Store();//保存IFeature对象  
            fr.WriteFeature(f);//将IFeature对象，添加到当前图层上  
            w.StopEditOperation();//停止编辑操作  
            w.StopEditing(true);//关闭编辑状态，并保存修改  
            //this.axMapControl1.Refresh();//刷新地图  
        }

        public static ISimpleMarkerSymbol GetPointStyle()
        {
            //创建SimpleMarkerSymbolClass对象

            ISimpleMarkerSymbol pSimpleMarkerSymbol = new SimpleMarkerSymbolClass();

            //创建RgbColorClass对象为pSimpleMarkerSymbol设置颜色

            IRgbColor pRgbColor = new RgbColorClass();

            pRgbColor.Red = 255;

            pSimpleMarkerSymbol.Color = pRgbColor as IColor;

            //设置pSimpleMarkerSymbol对象的符号类型，选择钻石

            pSimpleMarkerSymbol.Style = esriSimpleMarkerStyle.esriSMSCircle;

            //设置pSimpleMarkerSymbol对象大小，设置为５

            pSimpleMarkerSymbol.Size = 15;

            //显示外框线

            pSimpleMarkerSymbol.Outline = true;

            //为外框线设置颜色

            IRgbColor pLineRgbColor = new RgbColorClass();

            pLineRgbColor.Green = 255;

            pSimpleMarkerSymbol.OutlineColor = pLineRgbColor as IColor;

            //设置外框线的宽度

            pSimpleMarkerSymbol.OutlineSize = 1;
            return pSimpleMarkerSymbol;
        }

        public static IMarkerLineSymbol GetLineStyle()
        {
            IMarkerLineSymbol pMarkerLine = new MarkerLineSymbol();

            IRgbColor pLineColor = new RgbColorClass();

            pLineColor.Blue = 255;

            pMarkerLine.Color = pLineColor as IColor;
            pMarkerLine.Width = 3;
            return pMarkerLine;
        }
    }
}