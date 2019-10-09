﻿using SENAME.Senainfo.ModFichaResidencial.DAL.DAO;
using System;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Services;
using System.Xml;

namespace WS_SENAME
{
   

    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ToolboxItem(false)]
    public class Service : WebService
    {
        


        [WebMethod(Description = "Historial de los niños, niñas ingresados a los centros o proyectos de Protección, fechas, causas, tribunales.")]
        public XmlDocument HistorialNino_LRPA_Proteccion_xml(int RUN, string DV)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["connSQL"].ConnectionString;
            var objconn = new SqlConnection {ConnectionString = connectionString};
            var dataNna = this.GetDataNNA(RUN, DV);
            var CodNino = 0;
            if (dataNna.Rows.Count > 0)
                CodNino = Convert.ToInt32(dataNna.Rows[0]["CodNino"].ToString());
            var ninoHistorial = GetNinoHistorial(objconn, CodNino);
            objconn.Close();
            return ninoHistorial;
        }

        [WebMethod(Description = "Ficha Individual. Información con relación a la situación de los niño niña o adolescente asociada a su estadía en un Centro Residencial o Familia de Acogida.")]
        public XmlDocument FichaIndividual_xml(int RUN, string DV)
        {
            DataTable dataTable = new DataTable();
            SqlConnection sqlConnection = new SqlConnection(ConfigurationManager.ConnectionStrings["Conexiones"].ConnectionString);
            SqlCommand selectCommand = new SqlCommand("Get_WS_CargaDatosNNA", sqlConnection);
            selectCommand.CommandTimeout = 100000;
            selectCommand.CommandType = CommandType.StoredProcedure;
            selectCommand.Parameters.Add("@RUN", SqlDbType.VarChar).Value = (object)RUN;
            selectCommand.Parameters.Add("@DV", SqlDbType.VarChar).Value = (object)DV;
            new SqlDataAdapter(selectCommand).Fill(dataTable);
            XmlDocument xmlDocument = new XmlDocument();
            return this.GetNinoFichaIndividual(sqlConnection, dataTable);
        }

        [WebMethod(Description = "Ficha Individual TEST. Información con relación a la situación de los niño niña o adolescente asociada a su estadía en un Centro Residencial o Familia de Acogida.")]
        public XmlDocument FichaIndividual_xml_TEST(int RUN, string DV)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["connSQL"].ConnectionString;
            SqlConnection sqlConnection = new SqlConnection();
            sqlConnection.ConnectionString = connectionString;
            SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(string.Empty + "SELECT top 1 Nombres, Apellido_Paterno, Apellido_Materno, Rut, dbo.f_rectificacion_cambia_fecha_str(FechaNacimiento) as FechaNacimiento, DATEDIFF(yyyy, FechaNacimiento, GETDATE()) as Edad, Sexo, CodNino, CodNacionalidad, (Select Descripcion from parNacionalidades pn where pn.CodNacionalidad = n.CodNacionalidad) as Nacionalidad FROM Ninos n " + "WHERE Rut = '" + RUN.ToString().Trim() + "-" + DV.Trim() + "' " + "order by CodNino desc", sqlConnection);
            DataTable dtNino = new DataTable();
            DataTable dataTable = dtNino;
            sqlDataAdapter.Fill(dataTable);
            XmlDocument xmlDocument = new XmlDocument();
            XmlDocument fichaIndividualTest = this.GetNinoFichaIndividual_TEST(sqlConnection, dtNino);
            sqlConnection.Close();
            return fichaIndividualTest;
        }

        [WebMethod(Description = "Información del Proyecto")]
        public XmlDocument InformacionProyecto_xml(int CodProyecto)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["connSQL"].ConnectionString;
            SqlConnection sqlConn = new SqlConnection();
            sqlConn.ConnectionString = connectionString;
            XmlDocument xmlDocument = new XmlDocument();
            XmlDocument datosProyecto = this.GetDatosProyecto(sqlConn, CodProyecto);
            sqlConn.Close();
            return datosProyecto;
        }

        //TODO Modificaciones por Héctor Gatica Marzo 2019

        [WebMethod(Description = "Ficha Individual (2019)")]
        public XmlDocument FichaIndividual_xml_Final(string rut)
        {
            if (string.IsNullOrEmpty(rut) || rut == "0")
            {
                var xmlDocument = new XmlDocument();
                var str1 = Guid.NewGuid().ToString();
               
                var str2 =  ConfigurationManager.AppSettings["PathXML"] + "Fichaindividual-" + str1 + ".xml"; 

                var xmlTextWriter = new XmlTextWriter(str2, Encoding.UTF8) {Formatting = Formatting.Indented};
                xmlTextWriter.WriteStartDocument();
                xmlTextWriter.WriteStartElement("Fichaindividual");
                xmlTextWriter.WriteElementString("Estado", "Debe ingresar rut");
                xmlTextWriter.WriteEndElement();
                xmlTextWriter.Flush();
                xmlTextWriter.Close();
                xmlDocument.Load(str2);
                File.Delete(str2);
                return xmlDocument;
            }

            var connectionString = ConfigurationManager.ConnectionStrings["Conexiones"].ConnectionString;
            var ds = new DataSet();

            using (var con = new SqlConnection(connectionString))
            {
                var cmd = new SqlCommand("WebServices_GetFichaIndividual",con)
                {
                    CommandTimeout = 10000000,
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.Add("@Rut", SqlDbType.VarChar).Value = rut;
                new SqlDataAdapter(cmd).Fill(ds);
                return GetDatosFichaIndividual(ds);

            }
         
        }

        private XmlDocument GetDatosFichaIndividual(DataSet ds)
        {
            var xmlDocument = new XmlDocument();
            var str1 = Guid.NewGuid().ToString();
            if (!Directory.Exists(ConfigurationManager.AppSettings["PathXML"]))
                Directory.CreateDirectory(ConfigurationManager.AppSettings["PathXML"]);

            var str2 = ConfigurationManager.AppSettings["PathXML"] + "FichaIndividual-" + str1 + ".xml";
            var xmlTextWriter = new XmlTextWriter(str2, Encoding.UTF8) { Formatting = Formatting.Indented };
            xmlTextWriter.WriteStartDocument();
            xmlTextWriter.WriteStartElement("FichaIndividual");

            if (ds.Tables[0].Rows.Count > 0)
            {
                xmlTextWriter.WriteStartElement("Estado");
                xmlTextWriter.WriteElementString("Estado", ds.Tables[0].Rows[0]["Estado"].ToString());
                xmlTextWriter.WriteEndElement();
                if (ds.Tables[1].Rows.Count > 0)
                {
                    xmlTextWriter.WriteElementString("Run", ds.Tables[1].Rows[0]["Rut"].ToString());
                    xmlTextWriter.WriteStartElement("DatosNNA");
                    xmlTextWriter.WriteElementString("nombres", ds.Tables[1].Rows[0]["nombres"].ToString());
                    xmlTextWriter.WriteElementString("Apellido_Paterno", ds.Tables[1].Rows[0]["Apellido_Paterno"].ToString());
                    xmlTextWriter.WriteElementString("Apellido_Materno", ds.Tables[1].Rows[0]["Apellido_Materno"].ToString());
                    xmlTextWriter.WriteElementString("FechaNacimiento", ds.Tables[1].Rows[0]["FechaNacimiento"].ToString());
                    xmlTextWriter.WriteElementString("Sexo", ds.Tables[1].Rows[0]["Sexo"].ToString());
                    xmlTextWriter.WriteElementString("CodNino", ds.Tables[1].Rows[0]["CodNino"].ToString());
                    xmlTextWriter.WriteElementString("CodProyecto", ds.Tables[1].Rows[0]["CodProyecto"].ToString());
                    xmlTextWriter.WriteElementString("NombreProyecto", ds.Tables[1].Rows[0]["NombreProyecto"].ToString());
                    xmlTextWriter.WriteElementString("FechaIngreso", ds.Tables[1].Rows[0]["FechaIngreso"].ToString());
                    xmlTextWriter.WriteElementString("FechaEgreso", ds.Tables[1].Rows[0]["FechaEgreso"].ToString());
                    xmlTextWriter.WriteEndElement();
                }
                xmlTextWriter.WriteStartElement("AntecedentesdeSalud");
                if (ds.Tables[2].Rows.Count > 0)
                {
                    for (var index = 0; index < ds.Tables[2].Rows.Count; ++index)
                    {
                        xmlTextWriter.WriteStartElement("EventosIntervencion");
                        xmlTextWriter.WriteElementString("INS_Consultorio", ds.Tables[2].Rows[index]["InscritoenConsultorio"].ToString());
                        xmlTextWriter.WriteElementString("INS_Consultorio_Des", ds.Tables[2].Rows[index]["InscritoenConsultorio_Des"].ToString());
                        xmlTextWriter.WriteElementString("FEC_UltimoControl", ds.Tables[2].Rows[index]["FechaUltimoControl"].ToString());
                        xmlTextWriter.WriteEndElement();
                    }
                }
                if (ds.Tables[3].Rows.Count > 0)
                {
                    for (var index = 0; index < ds.Tables[3].Rows.Count; ++index)
                    {
                        xmlTextWriter.WriteStartElement("EnfermedadesCronicas");
                        xmlTextWriter.WriteElementString("FLG_Dia_Med", ds.Tables[3].Rows[index]["EnfermedadCronicaDiagnosticoMedico"].ToString());
                        xmlTextWriter.WriteElementString("FLG_Dia_Med_Des", ds.Tables[3].Rows[index]["EnfermedadCronicaDiagnosticoMedico_Des"].ToString());
                        xmlTextWriter.WriteElementString("CodEnfermedadCronica", ds.Tables[3].Rows[index]["CodEnfermedadCronica"].ToString());
                        xmlTextWriter.WriteElementString("EnfermedadCronica", ds.Tables[3].Rows[index]["EnfermedadCronica"].ToString());
                        xmlTextWriter.WriteElementString("TRATA_Cronico", ds.Tables[3].Rows[index]["RecibeTratamiento"].ToString());
                        xmlTextWriter.WriteElementString("TRATA_Cronico_Des", ds.Tables[3].Rows[index]["RecibeTratamiento_Des"].ToString());
                        xmlTextWriter.WriteEndElement();
                    }
                }
                if (ds.Tables[4].Rows.Count > 0)
                {
                    for (var index = 0; index < ds.Tables[4].Rows.Count; ++index)
                    {
                        xmlTextWriter.WriteStartElement("SaludMental");
                        xmlTextWriter.WriteElementString("SALUD_Mental", ds.Tables[4].Rows[index]["PresentaProblemasdeSaludMental"].ToString());
                        xmlTextWriter.WriteElementString("SALUD_Mental_Des", ds.Tables[4].Rows[index]["PresentaProblemasdeSaludMental_Des"].ToString());
                        xmlTextWriter.WriteElementString("DIAGNOSTICADO", ds.Tables[4].Rows[index]["FueDiagnosticado"].ToString());
                        xmlTextWriter.WriteElementString("TRATA_Mental", ds.Tables[4].Rows[index]["RecibeTratamientoDiagnostico"].ToString());
                        xmlTextWriter.WriteElementString("ENTIDAD_Trata", ds.Tables[4].Rows[index]["QuienrealizaTratamiento"].ToString());
                        xmlTextWriter.WriteEndElement();
                    }
                }
                if (ds.Tables[5].Rows.Count > 0)
                {
                    for (var index = 0; index < ds.Tables[5].Rows.Count; ++index)
                    {
                        xmlTextWriter.WriteStartElement("Discapacidad");
                        xmlTextWriter.WriteElementString("TIEdis", ds.Tables[5].Rows[index]["TieneDiscapacidad"].ToString());
                        xmlTextWriter.WriteElementString("TRATAMIENTO_Dis", ds.Tables[5].Rows[index]["TieneDiscapacidad_Des"].ToString());
                        xmlTextWriter.WriteElementString("REGdis", ds.Tables[5].Rows[index]["Registrodediscapacidad"].ToString());
                        xmlTextWriter.WriteElementString("CodTipoDiscapacidad", ds.Tables[5].Rows[index]["CodTipoDiscapacidad"].ToString());
                        xmlTextWriter.WriteElementString("Discapacidad", ds.Tables[5].Rows[index]["TipoDiscapacidad"].ToString());
                        xmlTextWriter.WriteElementString("CodNivelDiscapacidad", ds.Tables[5].Rows[index]["CodNivelDiscapacidad"].ToString());
                        xmlTextWriter.WriteElementString("NivelDiscapacidad", ds.Tables[5].Rows[index]["NivelDiscapacidad"].ToString());
                        xmlTextWriter.WriteElementString("OBSERV_Salud", ds.Tables[5].Rows[index]["Observacion"].ToString());
                        xmlTextWriter.WriteEndElement();
                    }
                }
                xmlTextWriter.WriteEndElement();
                if (ds.Tables[6].Rows.Count > 0)
                {
                    xmlTextWriter.WriteStartElement("AntecedentesEscolaridad");
                    xmlTextWriter.WriteElementString("CodEscolaridad", ds.Tables[6].Rows[0]["CodCursoActual"].ToString());
                    xmlTextWriter.WriteElementString("Escolaridad", ds.Tables[6].Rows[0]["DescripcionCursoActual"].ToString());
                    xmlTextWriter.WriteElementString("ULT_Curso_Aprov", ds.Tables[6].Rows[0]["CodUltimoCursoAprobado"].ToString());
                    xmlTextWriter.WriteElementString("EscolaridadAprobada", ds.Tables[6].Rows[0]["DescripcionUltimoCursoAprobado"].ToString());
                    xmlTextWriter.WriteElementString("AgnoEscolar", ds.Tables[6].Rows[0]["AnoUltimoCursoAprobado"].ToString());
                    xmlTextWriter.WriteElementString("CodTipoAsistenciaEscolar", ds.Tables[6].Rows[0]["CodAsistenciaEscolar"].ToString());
                    xmlTextWriter.WriteElementString("TipoAsistenciaEscolar", ds.Tables[6].Rows[0]["AsistenciaEscolar"].ToString());
                    xmlTextWriter.WriteElementString("FLG_Ret_Esc_In", ds.Tables[6].Rows[0]["PresentaRetrasoEscolar"].ToString());
                    xmlTextWriter.WriteElementString("NUM_Niv_Dif", ds.Tables[6].Rows[0]["NivelDiferencial"].ToString());
                    xmlTextWriter.WriteElementString("INASIS_Op", ds.Tables[6].Rows[0]["INASIS_Op"].ToString());
                    xmlTextWriter.WriteElementString("OBSERV_Escolar", ds.Tables[6].Rows[0]["Observaciones"].ToString());
                    xmlTextWriter.WriteEndElement();
                }
                if (ds.Tables[7].Rows.Count > 0)
                {
                    xmlTextWriter.WriteStartElement("AntecedentesdeConsumo");
                    for (var index = 0; index < ds.Tables[7].Rows.Count; ++index)
                    {
                        xmlTextWriter.WriteElementString("CON_Drogas", ds.Tables[7].Rows[index]["ConsumeDroga"].ToString());
                        xmlTextWriter.WriteElementString("CodDroga", ds.Tables[7].Rows[index]["CodTipoDroga"].ToString());
                        xmlTextWriter.WriteElementString("Droga", ds.Tables[7].Rows[index]["DescripcionTipoDroga"].ToString());
                        xmlTextWriter.WriteElementString("CodTipoConsumoDroga", ds.Tables[7].Rows[index]["CodTipoConsumoDroga"].ToString());
                        xmlTextWriter.WriteElementString("TipoConsumoDroga", ds.Tables[7].Rows[index]["TipoConsumoDroga"].ToString());
                        xmlTextWriter.WriteElementString("DIAGNOS_Medico", ds.Tables[7].Rows[index]["TieneEvaluacionConsumo"].ToString());
                        xmlTextWriter.WriteElementString("TRATA_Drogas", ds.Tables[7].Rows[index]["TieneTratamiento"].ToString());
                        xmlTextWriter.WriteElementString("TIReh", ds.Tables[7].Rows[index]["TieneRehabilitacion"].ToString());
                        xmlTextWriter.WriteElementString("CodInstitucionRealizaDiagnostico", ds.Tables[7].Rows[index]["CodInstitucionRealizaDiagnostico"].ToString());
                        xmlTextWriter.WriteElementString("INST_Drogas", ds.Tables[7].Rows[index]["InstitucionRealizaDiagnostico"].ToString());
                        xmlTextWriter.WriteElementString("RESUL_Final", ds.Tables[7].Rows[index]["RESUL_Final"].ToString());
                        xmlTextWriter.WriteElementString("OBSERV_Consumo", ds.Tables[7].Rows[index]["Observacion"].ToString());
                    }
                    xmlTextWriter.WriteEndElement();
                }
                if (ds.Tables[8].Rows.Count > 0)
                {
                    xmlTextWriter.WriteStartElement("SituacionFamiliar");
                    for (var index = 0; index < ds.Tables[8].Rows.Count; ++index)
                    {
                        xmlTextWriter.WriteElementString("TRABAJO_Egreso", ds.Tables[8].Rows[index]["CodConQuienExisteTrabajoParaElEgreso"].ToString());
                        xmlTextWriter.WriteElementString("FEC_Desde", ds.Tables[8].Rows[index]["DesdeCuando"].ToString());
                        xmlTextWriter.WriteElementString("PROYECT_Realiza", ds.Tables[8].Rows[index]["CodQuienRealizaElTrabajo"].ToString());
                        xmlTextWriter.WriteElementString("COMUNA_Trabajo", ds.Tables[8].Rows[index]["CodComuna"].ToString());
                        xmlTextWriter.WriteElementString("OBSERV_Familiar", ds.Tables[8].Rows[index]["Observaciones"].ToString());
                    }
                    xmlTextWriter.WriteEndElement();
                }
                if (ds.Tables[9].Rows.Count > 0)
                {
                    xmlTextWriter.WriteStartElement("Visitas");
                    xmlTextWriter.WriteElementString("visitas", ds.Tables[9].Rows[0]["RecibeVisitas"].ToString());
                    xmlTextWriter.WriteElementString("visitantes", ds.Tables[9].Rows[0]["Madre"].ToString());
                    xmlTextWriter.WriteElementString("periocidad", ds.Tables[9].Rows[0]["Periodicidad"].ToString());
                    xmlTextWriter.WriteElementString("salidas", ds.Tables[9].Rows[0]["SalidasPernoctacion"].ToString());
                    xmlTextWriter.WriteElementString("acompanantes", ds.Tables[9].Rows[0]["Acompañante"].ToString());
                    xmlTextWriter.WriteEndElement();
                }
                if (ds.Tables[10].Rows.Count > 0)
                {
                    xmlTextWriter.WriteStartElement("ProcesodeIntervencion");
                    xmlTextWriter.WriteElementString("EVAL_Diagnos", ds.Tables[10].Rows[0]["EVAL_Diagnos"].ToString());
                    xmlTextWriter.WriteElementString("CON_Diagnos", ds.Tables[10].Rows[0]["CON_Diagnos"].ToString());
                    xmlTextWriter.WriteElementString("ENFOQUE_Plan", ds.Tables[10].Rows[0]["ENFOQUE_Plan"].ToString());
                    xmlTextWriter.WriteElementString("FEC_Ultimo", ds.Tables[10].Rows[0]["FEC_Ultimo"].ToString());
                    xmlTextWriter.WriteElementString("POSIBIL_Resti", ds.Tables[10].Rows[0]["POSIBIL_Resti"].ToString());
                    xmlTextWriter.WriteElementString("RES_Inform", ds.Tables[10].Rows[0]["RES_Inform"].ToString());
                    xmlTextWriter.WriteElementString("TIP_Interv", ds.Tables[10].Rows[0]["TIP_Interv"].ToString());
                    xmlTextWriter.WriteElementString("INST_Interv", ds.Tables[10].Rows[0]["INST_Interv"].ToString());
                    xmlTextWriter.WriteElementString("LIST_Amb", ds.Tables[10].Rows[0]["LIST_Amb"].ToString());
                    xmlTextWriter.WriteElementString("FLG_Mal_Res", ds.Tables[10].Rows[0]["FLG_Mal_Res"].ToString());
                    xmlTextWriter.WriteStartElement("MALTRATO");
                    for (var index = 0; index < ds.Tables[11].Rows.Count; ++index)
                        xmlTextWriter.WriteElementString("NUM_Tip_Mal", ds.Tables[11].Rows[index]["NUM_Tip_Mal"].ToString());
                    xmlTextWriter.WriteEndElement();
                    xmlTextWriter.WriteStartElement("PARTICIPACION");
                    for (var index = 0; index < ds.Tables[12].Rows.Count; ++index)
                        xmlTextWriter.WriteElementString("NUM_For_Par", ds.Tables[12].Rows[index]["NUM_For_Par"].ToString());
                    xmlTextWriter.WriteEndElement();
                    xmlTextWriter.WriteElementString("FLG_Inftribunal", ds.Tables[10].Rows[0]["FLG_Inftribunal"].ToString());
                    xmlTextWriter.WriteElementString("FLG_Medidas", ds.Tables[10].Rows[0]["FLG_Medidas"].ToString());
                    xmlTextWriter.WriteElementString("GLS_Cualesmed", ds.Tables[10].Rows[0]["GLS_Cualesmed"].ToString());
                    xmlTextWriter.WriteElementString("PLAZO_Pregreso", ds.Tables[10].Rows[0]["PLAZO_Pregreso"].ToString());
                    xmlTextWriter.WriteElementString("PLAZO_ACERCAMIENTO_DESCRIPCION", ds.Tables[10].Rows[0]["PLAZO_ACERCAMIENTO_DESCRIPCION"].ToString());
                    xmlTextWriter.WriteElementString("FEC_Pregreso", ds.Tables[10].Rows[0]["FEC_Pregreso"].ToString());
                    xmlTextWriter.WriteElementString("PLAN_INTERVENCION", ds.Tables[10].Rows[0]["PLAN_INTERVENCION"].ToString());
                    xmlTextWriter.WriteElementString("INTesp", ds.Tables[10].Rows[0]["INTesp"].ToString());
                    xmlTextWriter.WriteElementString("PRE_egreso", ds.Tables[10].Rows[0]["PRE_egreso"].ToString());
                    xmlTextWriter.WriteElementString("TIP_Resolucion", ds.Tables[10].Rows[0]["TIP_Resolucion"].ToString());
                    xmlTextWriter.WriteElementString("OBSERV_Egreso", ds.Tables[10].Rows[0]["OBSERV_Egreso"].ToString());
                    xmlTextWriter.WriteEndElement();
                }
                else
                {
                    xmlTextWriter.WriteStartElement("ProcesodeIntervencion");
                    xmlTextWriter.WriteElementString("EVAL_Diagnos", "");
                    xmlTextWriter.WriteElementString("CON_Diagnos", "");
                    xmlTextWriter.WriteElementString("ENFOQUE_Plan", "");
                    xmlTextWriter.WriteElementString("FEC_Ultimo", "");
                    xmlTextWriter.WriteElementString("POSIBIL_Resti", "");
                    xmlTextWriter.WriteElementString("RES_Inform", "");
                    xmlTextWriter.WriteElementString("TIP_Interv", "");
                    xmlTextWriter.WriteElementString("INST_Interv", "");
                    xmlTextWriter.WriteElementString("LIST_Amb", "");
                    xmlTextWriter.WriteElementString("FLG_Mal_Res", "");
                    xmlTextWriter.WriteStartElement("MALTRATO");
                    xmlTextWriter.WriteElementString("NUM_Tip_Mal", "");
                    xmlTextWriter.WriteEndElement();
                    xmlTextWriter.WriteStartElement("PARTICIPACION");
                    xmlTextWriter.WriteElementString("NUM_Tip_Mal", "");
                    xmlTextWriter.WriteEndElement();
                    xmlTextWriter.WriteElementString("FLG_Inftribunal", "");
                    xmlTextWriter.WriteElementString("FLG_Medidas", "");
                    xmlTextWriter.WriteElementString("GLS_Cualesmed", "");
                    xmlTextWriter.WriteElementString("PLAZO_Pregreso", "");
                    xmlTextWriter.WriteElementString("PLAZO_ACERCAMIENTO_DESCRIPCION", "");
                    xmlTextWriter.WriteElementString("FEC_Pregreso", "");
                    xmlTextWriter.WriteElementString("PLAN_INTERVENCION", "");
                    xmlTextWriter.WriteElementString("INTesp", "");
                    xmlTextWriter.WriteElementString("PRE_egreso", "");
                    xmlTextWriter.WriteElementString("TIP_Resolucion", "");
                    xmlTextWriter.WriteElementString("OBSERV_Egreso", "");
                    xmlTextWriter.WriteEndElement();
                }
                xmlTextWriter.WriteEndElement();
                xmlTextWriter.Flush();
                xmlTextWriter.Close();
                xmlDocument.Load(str2);
                File.Delete(str2);
                return xmlDocument;
            }
            xmlTextWriter.WriteStartElement("Estado");
            xmlTextWriter.WriteElementString("Estado", "Run no Encontrado");
            xmlTextWriter.WriteEndElement();
            xmlTextWriter.WriteEndElement();
            xmlTextWriter.Flush();
            xmlTextWriter.Close();
            xmlDocument.Load(str2);
            File.Delete(str2);
            return xmlDocument;
        }

        [WebMethod(Description = "Devuelve la ultima Ficha Residencial registrada al ingresar un codigo de proyecto vigente")]
        public XmlDocument GetFichaResidencialXML(string codProyecto)
        {
            XmlDocument xmlDocument = new XmlDocument();
            Regex regex = new Regex("^\\d+$");
            try
            {
                if (regex.Match(codProyecto).Success)
                    xmlDocument = new FichaResidencialMasivoDao().ObtenerDatosFichaResidencialXML(int.Parse(codProyecto));
                else
                    xmlDocument.LoadXml("<FICHA_RESIDENCIAL><ESTATUS><CODIGO>4</CODIGO><GLOSA>VALOR DE PARÁMETRO NO ESTÁ DENTRO DEL DOMINIO DE VALORES PERMITIDOS (ENTEROS).</GLOSA></ESTATUS></FICHA_RESIDENCIAL>");
            }
            catch (Exception ex)
            {
                xmlDocument.LoadXml("<FICHA_RESIDENCIAL><ESTATUS><CODIGO>4</CODIGO><GLOSA>" + ex.Message + "</GLOSA></ESTATUS></FICHA_RESIDENCIAL>");
            }
            return xmlDocument;
        }

        [WebMethod(Description = "Devuelve la ultima Ficha Residencial con Observaciones (PJUD) y/o Respuestas (SENAME)")]
        public XmlDocument FichaResidencial_ObservacionesRespuestasXML(string codProyecto)
        {
            XmlDocument xmlDocument = new XmlDocument();
            Regex regex = new Regex("^\\d+$");
            try
            {
                if (regex.Match(codProyecto).Success)
                    xmlDocument = new FichaResidencialMasivoDao().ObtenerDatosObservacionesFichaXML(int.Parse(codProyecto));
                else
                    xmlDocument.LoadXml("<OBSERVACIONES_FICHA_RESIDENCIAL><ESTATUS><CODIGO>5</CODIGO><GLOSA>VALOR DE PARÁMETRO NO ESTÁ DENTRO DEL DOMINIO DE VALORES PERMITIDOS (ENTEROS).</GLOSA></ESTATUS></OBSERVACIONES_FICHA_RESIDENCIAL>");
            }
            catch (Exception ex)
            {
                xmlDocument.LoadXml("<OBSERVACIONES_FICHA_RESIDENCIAL><ESTATUS><CODIGO>5</CODIGO><GLOSA>" + ex.Message + "</GLOSA></ESTATUS></OBSERVACIONES_FICHA_RESIDENCIAL>");
            }
            return xmlDocument;
        }

        [WebMethod(Description = "Al ingresar un codigo de proyecto indica si esta vigente. Si se ingresa 0 devuelve un listado de todos los proyectos vigentes por residencias")]
        public XmlDocument ProyectosVigentesResidenciasXML(string codProyecto)
        {
            var xmlDocument = new XmlDocument();
            var regex = new Regex("^\\d+$");
            try
            {
                if (regex.Match(codProyecto).Success)
                    xmlDocument = new FichaResidencialMasivoDao().ObtenerDatosProyectoXML(int.Parse(codProyecto));
                else
                    xmlDocument.LoadXml("<PROYECTOS_VIGENTE><ESTATUS><CODIGO>4</CODIGO><GLOSA>VALOR DE PARÁMETRO NO ESTÁ DENTRO DEL DOMINIO DE VALORES PERMITIDOS (ENTEROS).</GLOSA></ESTATUS></PROYECTOS_VIGENTE>");
            }
            catch (Exception ex)
            {
                xmlDocument.LoadXml("<PROYECTOS_VIGENTE><ESTATUS><CODIGO>4</CODIGO><GLOSA>" + ex.Message.ToUpper() + "</GLOSA></ESTATUS></PROYECTOS_VIGENTE>");
            }
            return xmlDocument;
        }

        private XmlDocument GetDatosProyecto(SqlConnection sqlConn, int CodProyecto)
        {
            XmlDocument xmlDocument = new XmlDocument();
            if (!Directory.Exists(ConfigurationManager.AppSettings["PathXML"].ToString()))
                Directory.CreateDirectory(ConfigurationManager.AppSettings["PathXML"].ToString());
            string filename = ConfigurationManager.AppSettings["PathXML"].ToString() + "InformacionProyecto-2012-v1-1.xml";
            XmlTextWriter xmlTextWriter1 = new XmlTextWriter(filename, Encoding.UTF8);
            SqlCommand selectCommand = new SqlCommand();
            selectCommand.Connection = sqlConn;
            selectCommand.CommandType = CommandType.StoredProcedure;
            selectCommand.CommandText = "reporte_proyectos";
            selectCommand.Parameters.Add("@region", SqlDbType.Int, 4).Value = (object)-1;
            selectCommand.Parameters.Add("@codinstitucion", SqlDbType.Int, 4).Value = (object)-1;
            selectCommand.Parameters.Add("@codproyecto", SqlDbType.Int, 4).Value = (object)CodProyecto;
            selectCommand.Parameters.Add("@fechainicio", SqlDbType.Date, 16).Value = (object)DateTime.Now;
            selectCommand.Parameters.Add("@fechatermino", SqlDbType.Date, 16).Value = (object)DateTime.Now;
            selectCommand.Parameters.Add("@reporte", SqlDbType.Int, 4).Value = (object)1;
            selectCommand.Parameters.Add("@tipo", SqlDbType.Int, 4).Value = (object)-1;
            SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(selectCommand);
            DataTable dataTable1 = new DataTable();
            sqlConn.Open();
            DataTable dataTable2 = dataTable1;
            sqlDataAdapter.Fill(dataTable2);
            sqlConn.Close();
            xmlTextWriter1.Formatting = Formatting.Indented;
            xmlTextWriter1.WriteStartDocument();
            xmlTextWriter1.WriteStartElement("InformacionProyecto");
            string empty = string.Empty;
            string str1 = dataTable1.Rows.Count <= 0 ? "PROYECTO NO ENCONTRADO" : "PROYECTO ENCONTRADO";
            xmlTextWriter1.WriteStartElement("Estado");
            xmlTextWriter1.WriteElementString("Estado", str1);
            XmlTextWriter xmlTextWriter2 = xmlTextWriter1;
            string localName = "Fecha";
            DateTime dateTime = DateTime.Now;
            dateTime = dateTime.Date;
            string str2 = dateTime.ToString("dd-MM-yyyy");
            xmlTextWriter2.WriteElementString(localName, str2);
            xmlTextWriter1.WriteEndElement();
            if (dataTable1.Rows.Count == 0)
            {
                xmlTextWriter1.WriteEndElement();
                xmlTextWriter1.Flush();
                xmlTextWriter1.Close();
                xmlDocument.Load(filename);
                return xmlDocument;
            }
            string str3 = !(dataTable1.Rows[0]["NumeroPlazas"].ToString() == "F") ? (!(dataTable1.Rows[0]["NumeroPlazas"].ToString() == "M") ? "AMBOS" : "MASCULINO") : "FEMENINO";
            xmlTextWriter1.WriteElementString(nameof(CodProyecto), dataTable1.Rows[0][nameof(CodProyecto)].ToString());
            xmlTextWriter1.WriteElementString("NombreProyecto", dataTable1.Rows[0]["Nombre"].ToString());
            xmlTextWriter1.WriteElementString("CodInstitucion", dataTable1.Rows[0]["CodInstitucion"].ToString());
            xmlTextWriter1.WriteElementString("NombreInstitucion", dataTable1.Rows[0]["NombreInstitucion"].ToString());
            xmlTextWriter1.WriteElementString("NombreDirector", dataTable1.Rows[0]["NombreDirector"].ToString());
            xmlTextWriter1.WriteElementString("ProfesionDirector", dataTable1.Rows[0]["ProfesionDirector"].ToString());
            xmlTextWriter1.WriteElementString("Direccion", dataTable1.Rows[0]["Direccion"].ToString());
            xmlTextWriter1.WriteElementString("Comuna", dataTable1.Rows[0]["Comuna"].ToString());
            xmlTextWriter1.WriteElementString("Telefono", dataTable1.Rows[0]["Telefono"].ToString());
            xmlTextWriter1.WriteElementString("CorreoElectronico", dataTable1.Rows[0]["Mail"].ToString());
            xmlTextWriter1.WriteElementString("PlazasConvenidas", dataTable1.Rows[0]["NumeroPlazas"].ToString());
            xmlTextWriter1.WriteElementString("Sexo", str3);
            xmlTextWriter1.WriteElementString("EdadMinima", dataTable1.Rows[0]["EdadMinima"].ToString());
            xmlTextWriter1.WriteElementString("EdadMaxima", dataTable1.Rows[0]["EdadMaxima"].ToString());
            xmlTextWriter1.WriteEndElement();
            xmlTextWriter1.Flush();
            xmlTextWriter1.Close();
            xmlDocument.Load(filename);
            return xmlDocument;
        }

        private DataTable GetDataNNA(int RUN, string DV)
        {
            DataTable dataTable = new DataTable();
            SqlCommand selectCommand = new SqlCommand("Get_WS_GetDataNNA", new SqlConnection(ConfigurationManager.ConnectionStrings["Conexiones"].ConnectionString));
            selectCommand.CommandTimeout = 100000;
            selectCommand.CommandType = CommandType.StoredProcedure;
            selectCommand.Parameters.Add("@RUN", SqlDbType.VarChar).Value = (object)RUN;
            selectCommand.Parameters.Add("@DV", SqlDbType.VarChar).Value = (object)DV;
            new SqlDataAdapter(selectCommand).Fill(dataTable);
            return dataTable;
        }

        private XmlDocument GetNinoFichaIndividual(SqlConnection objconn, DataTable dtNino)
        {
            XmlDocument xmlDocument = new XmlDocument();
            string str1 = Guid.NewGuid().ToString();
            if (!Directory.Exists(ConfigurationManager.AppSettings["PathXML"].ToString()))
                Directory.CreateDirectory(ConfigurationManager.AppSettings["PathXML"].ToString());
            string str2 = ConfigurationManager.AppSettings["PathXML"].ToString() + "FichaIndividual-2012-v1-1" + str1 + ".xml";
            XmlTextWriter xmlTextWriter1 = new XmlTextWriter(str2, Encoding.UTF8);
            xmlTextWriter1.Formatting = Formatting.Indented;
            xmlTextWriter1.WriteStartDocument();
            xmlTextWriter1.WriteStartElement("FichaIndividual");
            string empty = string.Empty;
            string str3 = dtNino.Rows.Count <= 0 ? "RUN NO ENCONTRADO" : "RUN ENCONTRADO";
            xmlTextWriter1.WriteStartElement("Estado");
            xmlTextWriter1.WriteElementString("Estado", str3);
            XmlTextWriter xmlTextWriter2 = xmlTextWriter1;
            string localName = "Fecha";
            DateTime dateTime = DateTime.Now;
            dateTime = dateTime.Date;
            string str4 = dateTime.ToString("dd-MM-yyyy");
            xmlTextWriter2.WriteElementString(localName, str4);
            xmlTextWriter1.WriteEndElement();
            if (dtNino.Rows.Count == 0)
            {
                xmlTextWriter1.WriteEndElement();
                xmlTextWriter1.Flush();
                xmlTextWriter1.Close();
                xmlDocument.Load(str2);
                File.Delete(str2);
                return xmlDocument;
            }
            xmlTextWriter1.WriteStartElement("NinoNinaAdolescente");
            xmlTextWriter1.WriteElementString("CodNino", dtNino.Rows[0]["CodNino"].ToString());
            xmlTextWriter1.WriteStartElement("Run");
            xmlTextWriter1.WriteElementString("numero", dtNino.Rows[0]["Rut"].ToString().Substring(0, 8));
            xmlTextWriter1.WriteElementString("dv", dtNino.Rows[0]["Rut"].ToString().Substring(9, 1));
            xmlTextWriter1.WriteEndElement();
            xmlTextWriter1.WriteStartElement("Nombre");
            xmlTextWriter1.WriteElementString("nombres", dtNino.Rows[0]["Nombres"].ToString());
            xmlTextWriter1.WriteElementString("apellidoPaterno", dtNino.Rows[0]["Apellido_Paterno"].ToString());
            xmlTextWriter1.WriteElementString("apellidoMaterno", dtNino.Rows[0]["Apellido_Materno"].ToString());
            xmlTextWriter1.WriteEndElement();
            xmlTextWriter1.WriteElementString("FechaNacimiento", dtNino.Rows[0]["FechaNacimiento"].ToString());
            xmlTextWriter1.WriteElementString("Edad", dtNino.Rows[0]["Edad"].ToString());
            xmlTextWriter1.WriteElementString("Sexo", dtNino.Rows[0]["Sexo"].ToString());
            DataTable dataTable1 = this.CargaDatosIngreso(dtNino.Rows[0]["CodNino"].ToString());
            if (dataTable1.Rows.Count == 0)
            {
                xmlTextWriter1.WriteEndElement();
                xmlTextWriter1.Flush();
                xmlTextWriter1.Close();
                xmlDocument.Load(str2);
                File.Delete(str2);
                return xmlDocument;
            }
            string ICodIE = dataTable1.Rows[0]["ICodIE"].ToString();
            DataTable dataTable2 = this.CargaPadres(ICodIE);
            DataTable dataTable3 = this.CargaDatosDiscapacidad(ICodIE);
            DataTable dataTable4 = this.CargaDatosEnfermedadCronica(ICodIE);
            DataTable dataTable5 = this.CargaDatosEscolaridad(ICodIE);
            DataTable dataTable6 = this.CargaDatosDrogas(ICodIE);
            DataTable dataTable7 = this.CargaDatosPlanIntervencion(ICodIE);
            xmlTextWriter1.WriteStartElement("Padres");
            if (dataTable2.Rows.Count > 0)
            {
                xmlTextWriter1.WriteStartElement("Padre");
                xmlTextWriter1.WriteStartElement("Run");
                if (dataTable2.Rows[0]["RutPadre"].ToString() != "" && dataTable2.Rows[0]["RutPadre"].ToString().Length > 3)
                {
                    string str5 = dataTable2.Rows[0]["RutPadre"].ToString();
                    xmlTextWriter1.WriteElementString("numero", str5.Substring(0, str5.Length - 2));
                    xmlTextWriter1.WriteElementString("dv", str5.Substring(str5.Length - 1, 1));
                }
                else
                {
                    xmlTextWriter1.WriteElementString("numero", "");
                    xmlTextWriter1.WriteElementString("dv", "");
                }
                xmlTextWriter1.WriteEndElement();
                xmlTextWriter1.WriteStartElement("Nombre");
                if (dataTable2.Rows[0]["NombrePadre"].ToString() != "")
                {
                    xmlTextWriter1.WriteElementString("nombres", dataTable2.Rows[0]["NombrePadre"].ToString());
                    xmlTextWriter1.WriteElementString("apellidoPaterno", dataTable2.Rows[0]["ApellidoPaternoPadre"].ToString());
                    xmlTextWriter1.WriteElementString("apellidoMaterno", dataTable2.Rows[0]["ApellidoMaternoPadre"].ToString());
                }
                else
                {
                    xmlTextWriter1.WriteElementString("nombres", "NO REGISTRA INFORMACIÓN");
                    xmlTextWriter1.WriteElementString("apellidoPaterno", "NO REGISTRA INFORMACIÓN");
                    xmlTextWriter1.WriteElementString("apellidoMaterno", "NO REGISTRA INFORMACIÓN");
                }
                xmlTextWriter1.WriteEndElement();
                xmlTextWriter1.WriteEndElement();
                xmlTextWriter1.WriteStartElement("Madre");
                xmlTextWriter1.WriteStartElement("Run");
                if (dataTable2.Rows[0]["RutMadre"].ToString() != "" && dataTable2.Rows[0]["RutMadre"].ToString().Length > 3)
                {
                    string str5 = dataTable2.Rows[0]["RutMadre"].ToString();
                    xmlTextWriter1.WriteElementString("numero", str5.Substring(0, str5.Length - 2));
                    xmlTextWriter1.WriteElementString("dv", str5.Substring(str5.Length - 1, 1));
                }
                else
                {
                    xmlTextWriter1.WriteElementString("numero", "");
                    xmlTextWriter1.WriteElementString("dv", "");
                }
                xmlTextWriter1.WriteEndElement();
                xmlTextWriter1.WriteStartElement("Nombre");
                if (dataTable2.Rows[0]["NombreMadre"].ToString() != "")
                {
                    xmlTextWriter1.WriteElementString("nombres", dataTable2.Rows[0]["NombreMadre"].ToString());
                    xmlTextWriter1.WriteElementString("apellidoPaterno", dataTable2.Rows[0]["ApellidoPaternoMadre"].ToString());
                    xmlTextWriter1.WriteElementString("apellidoMaterno", dataTable2.Rows[0]["ApellidoMaternoMadre"].ToString());
                }
                else
                {
                    xmlTextWriter1.WriteElementString("nombres", "NO REGISTRA INFORMACIÓN");
                    xmlTextWriter1.WriteElementString("apellidoPaterno", "NO REGISTRA INFORMACIÓN");
                    xmlTextWriter1.WriteElementString("apellidoMaterno", "NO REGISTRA INFORMACIÓN");
                }
                xmlTextWriter1.WriteEndElement();
                xmlTextWriter1.WriteEndElement();
            }
            else
            {
                xmlTextWriter1.WriteStartElement("Padre");
                xmlTextWriter1.WriteStartElement("Run");
                xmlTextWriter1.WriteElementString("numero", "");
                xmlTextWriter1.WriteElementString("dv", "");
                xmlTextWriter1.WriteEndElement();
                xmlTextWriter1.WriteStartElement("Nombre");
                xmlTextWriter1.WriteElementString("nombres", "NO REGISTRA INFORMACIÓN");
                xmlTextWriter1.WriteElementString("apellidoPaterno", "NO REGISTRA INFORMACIÓN");
                xmlTextWriter1.WriteElementString("apellidoMaterno", "NO REGISTRA INFORMACIÓN");
                xmlTextWriter1.WriteEndElement();
                xmlTextWriter1.WriteEndElement();
                xmlTextWriter1.WriteStartElement("Madre");
                xmlTextWriter1.WriteStartElement("Run");
                xmlTextWriter1.WriteElementString("numero", "");
                xmlTextWriter1.WriteElementString("dv", "");
                xmlTextWriter1.WriteEndElement();
                xmlTextWriter1.WriteStartElement("Nombre");
                xmlTextWriter1.WriteElementString("nombres", "NO REGISTRA INFORMACIÓN");
                xmlTextWriter1.WriteElementString("apellidoPaterno", "NO REGISTRA INFORMACIÓN");
                xmlTextWriter1.WriteElementString("apellidoMaterno", "NO REGISTRA INFORMACIÓN");
                xmlTextWriter1.WriteEndElement();
                xmlTextWriter1.WriteEndElement();
            }
            xmlTextWriter1.WriteEndElement();
            xmlTextWriter1.WriteStartElement("HermanosEnElSistema");
            DataTable dataTable8 = this.CargaHermanosEnElSistema(dtNino.Rows[0]["CodNino"].ToString());
            if (dataTable8.Rows.Count > 0)
            {
                for (int index = 0; index < dataTable8.Rows.Count; ++index)
                {
                    xmlTextWriter1.WriteStartElement("Nombre");
                    xmlTextWriter1.WriteElementString("nombres", dataTable8.Rows[index]["Nombres"].ToString());
                    xmlTextWriter1.WriteElementString("apellidoPaterno", dataTable8.Rows[index]["Apellido_Paterno"].ToString());
                    xmlTextWriter1.WriteElementString("apellidoMaterno", dataTable8.Rows[index]["Apellido_Materno"].ToString());
                    xmlTextWriter1.WriteEndElement();
                }
            }
            else
            {
                xmlTextWriter1.WriteStartElement("Nombre");
                xmlTextWriter1.WriteElementString("nombres", "NO REGISTRA INFORMACIÓN");
                xmlTextWriter1.WriteElementString("apellidoPaterno", "NO REGISTRA INFORMACIÓN");
                xmlTextWriter1.WriteElementString("apellidoMaterno", "NO REGISTRA INFORMACIÓN");
                xmlTextWriter1.WriteEndElement();
            }
            xmlTextWriter1.WriteEndElement();
            xmlTextWriter1.WriteStartElement("Salud");
            if (dataTable3.Rows.Count > 0)
            {
                for (int index = 0; index < dataTable3.Rows.Count; ++index)
                {
                    xmlTextWriter1.WriteStartElement("Discapacidad");
                    xmlTextWriter1.WriteElementString("CodTipoDiscapacidad", dataTable3.Rows[index]["CodTipoDiscapacidad"].ToString());
                    xmlTextWriter1.WriteElementString("Discapacidad", dataTable3.Rows[index]["Discapacidad"].ToString());
                    xmlTextWriter1.WriteElementString("CodNivelDiscapacidad", dataTable3.Rows[index]["CodNivelDiscapacidad"].ToString());
                    xmlTextWriter1.WriteElementString("NivelDiscapacidad", dataTable3.Rows[index]["NivelDiscapacidad"].ToString());
                    xmlTextWriter1.WriteEndElement();
                }
            }
            else
            {
                xmlTextWriter1.WriteStartElement("Discapacidad");
                xmlTextWriter1.WriteElementString("CodTipoDiscapacidad", "");
                xmlTextWriter1.WriteElementString("Discapacidad", "NO REGISTRA INFORMACIÓN");
                xmlTextWriter1.WriteElementString("CodNivelDiscapacidad", "");
                xmlTextWriter1.WriteElementString("NivelDiscapacidad", "NO REGISTRA INFORMACIÓN");
                xmlTextWriter1.WriteEndElement();
            }
            if (dataTable4.Rows.Count > 0)
            {
                for (int index = 0; index < dataTable4.Rows.Count; ++index)
                {
                    xmlTextWriter1.WriteStartElement("EnfermedadCronica");
                    xmlTextWriter1.WriteElementString("CodEnfermedadCronica", dataTable4.Rows[index]["CodEnfermedadCronica"].ToString());
                    xmlTextWriter1.WriteElementString("EnfermedadCronica", dataTable4.Rows[index]["EnfermedadCronica"].ToString());
                    xmlTextWriter1.WriteEndElement();
                }
            }
            else
            {
                xmlTextWriter1.WriteStartElement("EnfermedadCronica");
                xmlTextWriter1.WriteElementString("CodEnfermedadCronica", "");
                xmlTextWriter1.WriteElementString("EnfermedadCronica", "NO REGISTRA INFORMACIÓN");
                xmlTextWriter1.WriteEndElement();
            }
            xmlTextWriter1.WriteEndElement();
            xmlTextWriter1.WriteStartElement("Escolaridad");
            if (dataTable5.Rows.Count > 0)
            {
                xmlTextWriter1.WriteElementString("CodEscolaridad", dataTable5.Rows[0]["CodEscolaridad"].ToString());
                xmlTextWriter1.WriteElementString("Escolaridad", dataTable5.Rows[0]["Escolaridad"].ToString());
                xmlTextWriter1.WriteElementString("CodTipoAsistenciaEscolar", dataTable5.Rows[0]["CodTipoAsistenciaEscolar"].ToString());
                xmlTextWriter1.WriteElementString("TipoAsistenciaEscolar", dataTable5.Rows[0]["TipoAsistenciaEscolar"].ToString());
                xmlTextWriter1.WriteElementString("AgnoEscolar", dataTable5.Rows[0]["AnoUltimoCursoAprobado"].ToString());
            }
            else
            {
                xmlTextWriter1.WriteElementString("CodEscolaridad", "");
                xmlTextWriter1.WriteElementString("Escolaridad", "NO REGISTRA INFORMACIÓN");
                xmlTextWriter1.WriteElementString("CodTipoAsistenciaEscolar", "");
                xmlTextWriter1.WriteElementString("TipoAsistenciaEscolar", "NO REGISTRA INFORMACIÓN");
                xmlTextWriter1.WriteElementString("AgnoEscolar", "");
            }
            xmlTextWriter1.WriteEndElement();
            xmlTextWriter1.WriteStartElement("Drogas");
            if (dataTable6.Rows.Count > 0)
            {
                xmlTextWriter1.WriteElementString("CodDroga", dataTable6.Rows[0]["CodDroga"].ToString());
                xmlTextWriter1.WriteElementString("Droga", dataTable6.Rows[0]["Droga"].ToString());
                xmlTextWriter1.WriteElementString("CodTipoConsumoDroga", dataTable6.Rows[0]["CodTipoConsumoDroga"].ToString());
                xmlTextWriter1.WriteElementString("TipoConsumoDroga", dataTable6.Rows[0]["TipoConsumoDroga"].ToString());
            }
            else
            {
                xmlTextWriter1.WriteElementString("CodDroga", "");
                xmlTextWriter1.WriteElementString("Droga", "NO REGISTRA INFORMACIÓN");
                xmlTextWriter1.WriteElementString("CodTipoConsumoDroga", "");
                xmlTextWriter1.WriteElementString("TipoConsumoDroga", "NO REGISTRA INFORMACIÓN");
            }
            xmlTextWriter1.WriteEndElement();
            xmlTextWriter1.WriteStartElement("PlanIntervencion");
            if (dataTable7.Rows.Count > 0)
            {
                xmlTextWriter1.WriteElementString("FechaInicioPlanIntervencion", dataTable7.Rows[0]["FechaInicioPlanIntervencion"].ToString());
                xmlTextWriter1.WriteElementString("FechaTerminoEstimadaPlanIntervencion", dataTable7.Rows[0]["FechaTerminoEstimadaPlanIntervencion"].ToString());
            }
            else
            {
                xmlTextWriter1.WriteElementString("FechaInicioPlanIntervencion", "");
                xmlTextWriter1.WriteElementString("FechaTerminoEstimadaPlanIntervencion", "");
            }
            xmlTextWriter1.WriteEndElement();
            xmlTextWriter1.WriteStartElement("Comuna");
            if (dataTable1.Rows.Count > 0 && dataTable1.Rows[0]["CodigoComunaNino"].ToString() != "")
            {
                xmlTextWriter1.WriteElementString("CodigoComuna", dataTable1.Rows[0]["CodigoComunaNino"].ToString());
                xmlTextWriter1.WriteElementString("DescripcionComuna", dataTable1.Rows[0]["ComunaNino"].ToString());
            }
            else
            {
                xmlTextWriter1.WriteElementString("CodigoComuna", "");
                xmlTextWriter1.WriteElementString("DescripcionComuna", "NO REGISTRA INFORMACIÓN");
            }
            xmlTextWriter1.WriteEndElement();
            xmlTextWriter1.WriteEndElement();
            xmlTextWriter1.WriteStartElement("Ingreso");
            xmlTextWriter1.WriteElementString("FechaIngreso", dataTable1.Rows[0]["FechaIngreso"].ToString());
            xmlTextWriter1.WriteElementString("FechaIngresoAlSistema", dataTable1.Rows[0]["FechaIngresoAlSistema"].ToString());
            xmlTextWriter1.WriteStartElement("SolicitanteIngreso");
            xmlTextWriter1.WriteElementString("CodSolicitanteIngreso", dataTable1.Rows[0]["CodSolicitanteIngreso"].ToString());
            xmlTextWriter1.WriteElementString("SolicitanteIngreso", dataTable1.Rows[0]["SolicitanteIngreso"].ToString());
            xmlTextWriter1.WriteEndElement();
            xmlTextWriter1.WriteStartElement("Proyecto");
            xmlTextWriter1.WriteElementString("CodProyecto", dataTable1.Rows[0]["CodProyecto"].ToString());
            xmlTextWriter1.WriteElementString("Proyecto", dataTable1.Rows[0]["Proyecto"].ToString());
            xmlTextWriter1.WriteStartElement("Comuna");
            if (dataTable1.Rows.Count > 0 && dataTable1.Rows[0]["CodigoComunaProyecto"].ToString() != "")
            {
                xmlTextWriter1.WriteElementString("CodigoComuna", dataTable1.Rows[0]["CodigoComunaProyecto"].ToString());
                xmlTextWriter1.WriteElementString("DescripcionComuna", dataTable1.Rows[0]["ComunaProyecto"].ToString());
            }
            else
            {
                xmlTextWriter1.WriteElementString("CodigoComuna", "0");
                xmlTextWriter1.WriteElementString("DescripcionComuna", "NO REGISTRA INFORMACIÓN");
            }
            xmlTextWriter1.WriteEndElement();
            xmlTextWriter1.WriteEndElement();
            xmlTextWriter1.WriteEndElement();
            xmlTextWriter1.WriteStartElement("Parametricas");
            xmlTextWriter1.WriteStartElement("parProyectos");
            DataTable dataTable9 = this.CargaParametricaProyectos();
            for (int index = 0; index < dataTable9.Rows.Count; ++index)
            {
                xmlTextWriter1.WriteElementString("CodigoProyecto", dataTable9.Rows[index]["CodigoProyecto"].ToString());
                xmlTextWriter1.WriteElementString("Proyecto", dataTable9.Rows[index]["Proyecto"].ToString());
            }
            xmlTextWriter1.WriteEndElement();
            xmlTextWriter1.WriteStartElement("parSolicitanteIngreso");
            DataTable dataTable10 = this.CargaParametricaSolicitanteIngreso();
            for (int index = 0; index < dataTable10.Rows.Count; ++index)
            {
                xmlTextWriter1.WriteElementString("CodSolicitanteIngreso", dataTable10.Rows[index]["CodSolicitanteIngreso"].ToString());
                xmlTextWriter1.WriteElementString("SolicitanteIngreso", dataTable10.Rows[index]["SolicitanteIngreso"].ToString());
            }
            xmlTextWriter1.WriteEndElement();
            xmlTextWriter1.WriteStartElement("parTipoDiscapacidad");
            DataTable dataTable11 = this.CargaParametricaTipoDiscapacidad();
            for (int index = 0; index < dataTable11.Rows.Count; ++index)
            {
                xmlTextWriter1.WriteElementString("CodTipoDiscapacidad", dataTable11.Rows[index]["CodTipoDiscapacidad"].ToString());
                xmlTextWriter1.WriteElementString("TipoDiscapacidad", dataTable11.Rows[index]["TipoDiscapacidad"].ToString());
            }
            xmlTextWriter1.WriteEndElement();
            xmlTextWriter1.WriteStartElement("parNivelDiscapacidad");
            DataTable dataTable12 = this.CargaParametricaNivelDiscapacidad();
            for (int index = 0; index < dataTable12.Rows.Count; ++index)
            {
                xmlTextWriter1.WriteElementString("CodNivelDiscapacidad", dataTable12.Rows[index]["CodNivelDiscapacidad"].ToString());
                xmlTextWriter1.WriteElementString("NivelDiscapacidad", dataTable12.Rows[index]["NivelDiscapacidad"].ToString());
            }
            xmlTextWriter1.WriteEndElement();
            xmlTextWriter1.WriteStartElement("parEnfermedadesCronicas");
            DataTable dataTable13 = this.CargaParametricaEnfermedadesCronicas();
            for (int index = 0; index < dataTable13.Rows.Count; ++index)
            {
                xmlTextWriter1.WriteElementString("CodEnfermedadCronica", dataTable13.Rows[index]["CodEnfermedadCronica"].ToString());
                xmlTextWriter1.WriteElementString("EnfermedadCronica", dataTable13.Rows[index]["EnfermedadCronica"].ToString());
            }
            xmlTextWriter1.WriteEndElement();
            xmlTextWriter1.WriteStartElement("parEscolaridad");
            DataTable dataTable14 = this.CargaParametricaEscolaridad();
            for (int index = 0; index < dataTable14.Rows.Count; ++index)
            {
                xmlTextWriter1.WriteElementString("CodEscolaridad", dataTable14.Rows[index]["CodEscolaridad"].ToString());
                xmlTextWriter1.WriteElementString("Escolaridad", dataTable14.Rows[index]["Escolaridad"].ToString());
            }
            xmlTextWriter1.WriteEndElement();
            xmlTextWriter1.WriteStartElement("parTipoAsistenciaEscolar");
            DataTable dataTable15 = this.CargaParametricaTipoAsistenciaEscolar();
            for (int index = 0; index < dataTable15.Rows.Count; ++index)
            {
                xmlTextWriter1.WriteElementString("CodTipoAsistenciaEscolar", dataTable15.Rows[index]["CodTipoAsistenciaEscolar"].ToString());
                xmlTextWriter1.WriteElementString("TipoAsistenciaEscolar", dataTable15.Rows[index]["TipoAsistenciaEscolar"].ToString());
            }
            xmlTextWriter1.WriteEndElement();
            xmlTextWriter1.WriteStartElement("parDrogas");
            DataTable dataTable16 = this.CargaParametricaDrogas();
            for (int index = 0; index < dataTable16.Rows.Count; ++index)
            {
                xmlTextWriter1.WriteElementString("CodDroga", dataTable16.Rows[index]["CodDroga"].ToString());
                xmlTextWriter1.WriteElementString("Droga", dataTable16.Rows[index]["Droga"].ToString());
            }
            xmlTextWriter1.WriteEndElement();
            xmlTextWriter1.WriteStartElement("parTipoConsumoDroga");
            DataTable dataTable17 = this.CargaParametricaTipoConsumoDroga();
            for (int index = 0; index < dataTable17.Rows.Count; ++index)
            {
                xmlTextWriter1.WriteElementString("CodTipoConsumoDroga", dataTable17.Rows[index]["CodTipoConsumoDroga"].ToString());
                xmlTextWriter1.WriteElementString("TipoConsumoDroga", dataTable17.Rows[index]["TipoConsumoDroga"].ToString());
            }
            xmlTextWriter1.WriteEndElement();
            xmlTextWriter1.WriteEndElement();
            xmlTextWriter1.Flush();
            xmlTextWriter1.Close();
            xmlDocument.Load(str2);
            File.Delete(str2);
            return xmlDocument;
        }

        private DataTable CargaParametricaNacionalidad()
        {
            DataTable dataTable = new DataTable();
            SqlCommand selectCommand = new SqlCommand("Get_WS_CargaParametricaNacionalidad", new SqlConnection(ConfigurationManager.ConnectionStrings["Conexiones"].ConnectionString));
            selectCommand.CommandTimeout = 100000;
            selectCommand.CommandType = CommandType.StoredProcedure;
            new SqlDataAdapter(selectCommand).Fill(dataTable);
            return dataTable;
        }

        private DataTable CargaParametricaTipoConsumoDroga()
        {
            DataTable dataTable = new DataTable();
            SqlCommand selectCommand = new SqlCommand("Get_WS_CargaParametricaTipoConsumoDroga", new SqlConnection(ConfigurationManager.ConnectionStrings["Conexiones"].ConnectionString));
            selectCommand.CommandTimeout = 100000;
            selectCommand.CommandType = CommandType.StoredProcedure;
            new SqlDataAdapter(selectCommand).Fill(dataTable);
            return dataTable;
        }

        private DataTable CargaParametricaDrogas()
        {
            DataTable dataTable = new DataTable();
            SqlCommand selectCommand = new SqlCommand("Get_WS_CargaParametricaDrogas", new SqlConnection(ConfigurationManager.ConnectionStrings["Conexiones"].ConnectionString));
            selectCommand.CommandTimeout = 100000;
            selectCommand.CommandType = CommandType.StoredProcedure;
            new SqlDataAdapter(selectCommand).Fill(dataTable);
            return dataTable;
        }

        private DataTable CargaParametricaTipoAsistenciaEscolar()
        {
            DataTable dataTable = new DataTable();
            SqlCommand selectCommand = new SqlCommand("Get_WS_CargaParametricaTipoAsistenciaEscolar", new SqlConnection(ConfigurationManager.ConnectionStrings["Conexiones"].ConnectionString));
            selectCommand.CommandTimeout = 100000;
            selectCommand.CommandType = CommandType.StoredProcedure;
            new SqlDataAdapter(selectCommand).Fill(dataTable);
            return dataTable;
        }

        private DataTable CargaParametricaEscolaridad()
        {
            DataTable dataTable = new DataTable();
            SqlCommand selectCommand = new SqlCommand("Get_WS_CargaParametricaEscolaridad", new SqlConnection(ConfigurationManager.ConnectionStrings["Conexiones"].ConnectionString));
            selectCommand.CommandTimeout = 100000;
            selectCommand.CommandType = CommandType.StoredProcedure;
            new SqlDataAdapter(selectCommand).Fill(dataTable);
            return dataTable;
        }

        private DataTable CargaParametricaEnfermedadesCronicas()
        {
            DataTable dataTable = new DataTable();
            SqlCommand selectCommand = new SqlCommand("Get_WS_CargaParametricaEnfermedadesCronicas", new SqlConnection(ConfigurationManager.ConnectionStrings["Conexiones"].ConnectionString));
            selectCommand.CommandTimeout = 100000;
            selectCommand.CommandType = CommandType.StoredProcedure;
            new SqlDataAdapter(selectCommand).Fill(dataTable);
            return dataTable;
        }

        private DataTable CargaParametricaNivelDiscapacidad()
        {
            DataTable dataTable = new DataTable();
            SqlCommand selectCommand = new SqlCommand("Get_WS_CargaParametricaNivelDiscapacidad", new SqlConnection(ConfigurationManager.ConnectionStrings["Conexiones"].ConnectionString));
            selectCommand.CommandTimeout = 100000;
            selectCommand.CommandType = CommandType.StoredProcedure;
            new SqlDataAdapter(selectCommand).Fill(dataTable);
            return dataTable;
        }

        private DataTable CargaParametricaTipoDiscapacidad()
        {
            DataTable dataTable = new DataTable();
            SqlCommand selectCommand = new SqlCommand("Get_WS_CargaParametricaTipoDiscapacidad", new SqlConnection(ConfigurationManager.ConnectionStrings["Conexiones"].ConnectionString));
            selectCommand.CommandTimeout = 100000;
            selectCommand.CommandType = CommandType.StoredProcedure;
            new SqlDataAdapter(selectCommand).Fill(dataTable);
            return dataTable;
        }

        private DataTable CargaParametricaSolicitanteIngreso()
        {
            DataTable dataTable = new DataTable();
            SqlCommand selectCommand = new SqlCommand("Get_WS_CargaParametricaSolicitanteIngreso", new SqlConnection(ConfigurationManager.ConnectionStrings["Conexiones"].ConnectionString));
            selectCommand.CommandTimeout = 100000;
            selectCommand.CommandType = CommandType.StoredProcedure;
            new SqlDataAdapter(selectCommand).Fill(dataTable);
            return dataTable;
        }

        private DataTable CargaParametricaProyectos()
        {
            DataTable dataTable = new DataTable();
            SqlCommand selectCommand = new SqlCommand("Get_WS_CargaParametricaProyectos", new SqlConnection(ConfigurationManager.ConnectionStrings["Conexiones"].ConnectionString));
            selectCommand.CommandTimeout = 100000;
            selectCommand.CommandType = CommandType.StoredProcedure;
            new SqlDataAdapter(selectCommand).Fill(dataTable);
            return dataTable;
        }

        private DataTable CargaDatosPlanIntervencion(string ICodIE)
        {
            DataTable dataTable = new DataTable();
            SqlCommand selectCommand = new SqlCommand("Get_WS_CargaDatosPlanIntervencion", new SqlConnection(ConfigurationManager.ConnectionStrings["Conexiones"].ConnectionString));
            selectCommand.CommandTimeout = 100000;
            selectCommand.CommandType = CommandType.StoredProcedure;
            selectCommand.Parameters.Add("@Icodie", SqlDbType.VarChar).Value = (object)ICodIE;
            new SqlDataAdapter(selectCommand).Fill(dataTable);
            return dataTable;
        }

        private DataTable CargaDatosDrogas(string ICodIE)
        {
            DataTable dataTable = new DataTable();
            SqlCommand selectCommand = new SqlCommand("Get_WS_CargaDatosDrogas", new SqlConnection(ConfigurationManager.ConnectionStrings["Conexiones"].ConnectionString));
            selectCommand.CommandTimeout = 100000;
            selectCommand.CommandType = CommandType.StoredProcedure;
            selectCommand.Parameters.Add("@Icodie", SqlDbType.VarChar).Value = (object)ICodIE;
            new SqlDataAdapter(selectCommand).Fill(dataTable);
            return dataTable;
        }

        private DataTable CargaDatosEscolaridad(string ICodIE)
        {
            DataTable dataTable = new DataTable();
            SqlCommand selectCommand = new SqlCommand("Get_WS_CargaDatosEscolaridad", new SqlConnection(ConfigurationManager.ConnectionStrings["Conexiones"].ConnectionString));
            selectCommand.CommandTimeout = 100000;
            selectCommand.CommandType = CommandType.StoredProcedure;
            selectCommand.Parameters.Add("@Icodie", SqlDbType.VarChar).Value = (object)ICodIE;
            new SqlDataAdapter(selectCommand).Fill(dataTable);
            return dataTable;
        }

        private DataTable CargaDatosEnfermedadCronica(string ICodIE)
        {
            DataTable dataTable = new DataTable();
            SqlCommand selectCommand = new SqlCommand("Get_WS_CargaDatosEnfermedadCronica", new SqlConnection(ConfigurationManager.ConnectionStrings["Conexiones"].ConnectionString));
            selectCommand.CommandTimeout = 100000;
            selectCommand.CommandType = CommandType.StoredProcedure;
            selectCommand.Parameters.Add("@Icodie", SqlDbType.VarChar).Value = (object)ICodIE;
            new SqlDataAdapter(selectCommand).Fill(dataTable);
            return dataTable;
        }

        private DataTable CargaDatosDiscapacidad(string ICodIE)
        {
            DataTable dataTable = new DataTable();
            SqlCommand selectCommand = new SqlCommand("Get_WS_CargaDatosDiscapacidad", new SqlConnection(ConfigurationManager.ConnectionStrings["Conexiones"].ConnectionString));
            selectCommand.CommandTimeout = 100000;
            selectCommand.CommandType = CommandType.StoredProcedure;
            selectCommand.Parameters.Add("@Icodie", SqlDbType.VarChar).Value = (object)ICodIE;
            new SqlDataAdapter(selectCommand).Fill(dataTable);
            return dataTable;
        }

        private DataTable CargaHermanosEnElSistema(string Codnino)
        {
            DataTable dataTable = new DataTable();
            SqlCommand selectCommand = new SqlCommand("Get_WS_CargaHermanosEnElSistema", new SqlConnection(ConfigurationManager.ConnectionStrings["Conexiones"].ConnectionString));
            selectCommand.CommandTimeout = 100000;
            selectCommand.CommandType = CommandType.StoredProcedure;
            selectCommand.Parameters.Add("@Codnino", SqlDbType.VarChar).Value = (object)Codnino;
            new SqlDataAdapter(selectCommand).Fill(dataTable);
            return dataTable;
        }

        private DataTable CargaDatosIngreso(string Codnino)
        {
            DataTable dataTable = new DataTable();
            SqlCommand selectCommand = new SqlCommand("Get_WS_CargaDatosIngreso", new SqlConnection(ConfigurationManager.ConnectionStrings["Conexiones"].ConnectionString));
            selectCommand.CommandTimeout = 100000;
            selectCommand.CommandType = CommandType.StoredProcedure;
            selectCommand.Parameters.Add("@Codnino", SqlDbType.VarChar).Value = (object)Codnino;
            new SqlDataAdapter(selectCommand).Fill(dataTable);
            return dataTable;
        }

        private DataTable CargaPadres(string ICodIE)
        {
            DataTable dataTable = new DataTable();
            SqlCommand selectCommand = new SqlCommand("Get_WS_CargaPadres", new SqlConnection(ConfigurationManager.ConnectionStrings["Conexiones"].ConnectionString));
            selectCommand.CommandTimeout = 100000;
            selectCommand.CommandType = CommandType.StoredProcedure;
            selectCommand.Parameters.Add("@Icodie", SqlDbType.VarChar).Value = (object)ICodIE;
            new SqlDataAdapter(selectCommand).Fill(dataTable);
            return dataTable;
        }

        private XmlDocument GetNinoHistorial(SqlConnection objconn, int CodNino)
        {
            XmlDocument xmlDocument = new XmlDocument();
            string str1 = Guid.NewGuid().ToString();
            SqlCommand selectCommand = new SqlCommand();
            SqlDataAdapter sqlDataAdapter1 = new SqlDataAdapter(selectCommand);
            DataTable dataTable1 = new DataTable();
            selectCommand.Connection = objconn;
            selectCommand.CommandType = CommandType.StoredProcedure;
            selectCommand.Parameters.Add("@CodNino", SqlDbType.Int, 4).Value = (object)CodNino;
            selectCommand.CommandText = "GetNinoProteccion_Jueces";
            SqlDataAdapter sqlDataAdapter2 = new SqlDataAdapter(selectCommand);
            DataTable dataTable2 = new DataTable();
            objconn.Open();
            DataTable dataTable3 = dataTable2;
            sqlDataAdapter2.Fill(dataTable3);
            objconn.Close();
            if (!Directory.Exists(ConfigurationManager.AppSettings["PathXML"].ToString()))
                Directory.CreateDirectory(ConfigurationManager.AppSettings["PathXML"].ToString());
            string str2 = ConfigurationManager.AppSettings["PathXML"].ToString() + "HistorialNino-2012-v1-1" + str1 + ".xml";
            XmlTextWriter xmlTextWriter1 = new XmlTextWriter(str2, Encoding.UTF8);
            xmlTextWriter1.Formatting = Formatting.Indented;
            xmlTextWriter1.WriteStartDocument();
            xmlTextWriter1.WriteStartElement("HistorialNino");
            string empty1 = string.Empty;
            string str3 = dataTable2.Rows.Count > 0 || dataTable1.Rows.Count > 0 ? "RUN ENCONTRADO" : "RUN NO ENCONTRADO";
            xmlTextWriter1.WriteStartElement("Estado");
            xmlTextWriter1.WriteElementString("Estado", str3);
            XmlTextWriter xmlTextWriter2 = xmlTextWriter1;
            string localName = "Fecha";
            DateTime dateTime = DateTime.Now;
            dateTime = dateTime.Date;
            string str4 = dateTime.ToString("dd-MM-yyyy");
            xmlTextWriter2.WriteElementString(localName, str4);
            xmlTextWriter1.WriteEndElement();
            if (dataTable1.Rows.Count == 0 && dataTable2.Rows.Count == 0)
            {
                xmlTextWriter1.WriteEndElement();
                xmlTextWriter1.Flush();
                xmlTextWriter1.Close();
                xmlDocument.Load(str2);
                File.Delete(str2);
                return xmlDocument;
            }
            if (dataTable1.Rows.Count > 0)
            {
                xmlTextWriter1.WriteStartElement("RunAdolescente");
                xmlTextWriter1.WriteElementString("numero", dataTable1.Rows[0]["RUN"].ToString().Substring(0, 8));
                xmlTextWriter1.WriteElementString("dv", dataTable1.Rows[0]["RUN"].ToString().Substring(9, 1));
                xmlTextWriter1.WriteEndElement();
                xmlTextWriter1.WriteStartElement("Nombre");
                xmlTextWriter1.WriteElementString("nombres", dataTable1.Rows[0]["Nombres"].ToString());
                xmlTextWriter1.WriteElementString("apellidoPaterno", dataTable1.Rows[0]["Apellido_Paterno"].ToString());
                xmlTextWriter1.WriteElementString("apellidoMaterno", dataTable1.Rows[0]["Apellido_Materno"].ToString());
                xmlTextWriter1.WriteEndElement();
                xmlTextWriter1.WriteElementString("FechaNacimiento", dataTable1.Rows[0]["FechaNacimiento"].ToString());
                xmlTextWriter1.WriteElementString("Edad", dataTable1.Rows[0]["Edad"].ToString());
                xmlTextWriter1.WriteElementString("Sexo", dataTable1.Rows[0]["Sexo"].ToString());
            }
            else
            {
                xmlTextWriter1.WriteStartElement("RunAdolescente");
                xmlTextWriter1.WriteElementString("numero", dataTable2.Rows[0]["RUN"].ToString().Substring(0, 8));
                xmlTextWriter1.WriteElementString("dv", dataTable2.Rows[0]["RUN"].ToString().Substring(9, 1));
                xmlTextWriter1.WriteEndElement();
                xmlTextWriter1.WriteStartElement("Nombre");
                xmlTextWriter1.WriteElementString("nombres", dataTable2.Rows[0]["Nombres"].ToString());
                xmlTextWriter1.WriteElementString("apellidoPaterno", dataTable2.Rows[0]["Apellido_Paterno"].ToString());
                xmlTextWriter1.WriteElementString("apellidoMaterno", dataTable2.Rows[0]["Apellido_Materno"].ToString());
                xmlTextWriter1.WriteEndElement();
                xmlTextWriter1.WriteElementString("FechaNacimiento", dataTable2.Rows[0]["FechaNacimiento"].ToString());
                xmlTextWriter1.WriteElementString("Edad", dataTable2.Rows[0]["Edad"].ToString());
                xmlTextWriter1.WriteElementString("Sexo", dataTable2.Rows[0]["Sexo"].ToString());
            }
            string empty2 = string.Empty;
            if (dataTable1.Rows.Count > 0)
            {
                xmlTextWriter1.WriteStartElement("HistoricoIngresosLRPA");
                for (int index = 0; index < dataTable1.Rows.Count; ++index)
                {
                    string str5 = !(dataTable1.Rows[index]["SancionAccesoria"].ToString() == "0") ? (!(dataTable1.Rows[index]["SancionAccesoria"].ToString() == "1") ? "SI" : "NO") : "NO";
                    xmlTextWriter1.WriteStartElement("HistoricoIngresosTO");
                    xmlTextWriter1.WriteElementString("CodigoIngreso", dataTable1.Rows[index]["ICodIE"].ToString());
                    xmlTextWriter1.WriteElementString("FechaIngreso", dataTable1.Rows[index]["FechaIngreso"].ToString().Substring(0, 10));
                    xmlTextWriter1.WriteElementString("CodigoProyecto", dataTable1.Rows[index]["CodProyecto"].ToString());
                    xmlTextWriter1.WriteElementString("Proyecto", dataTable1.Rows[index]["NombreProyecto"].ToString());
                    xmlTextWriter1.WriteElementString("CalidadJuridica", dataTable1.Rows[index]["CalidadJuridica"].ToString());
                    xmlTextWriter1.WriteElementString("CodigoTribunal", dataTable1.Rows[index]["CodigoTribunalCAPJ"].ToString());
                    xmlTextWriter1.WriteElementString("Tribunal", dataTable1.Rows[index]["Tribunal"].ToString());
                    xmlTextWriter1.WriteElementString("RolUnicoCausa", dataTable1.Rows[index]["Ruc"].ToString());
                    xmlTextWriter1.WriteElementString("RolInternoTribunal", dataTable1.Rows[index]["Rit"].ToString());
                    xmlTextWriter1.WriteElementString("CodigoDelito", dataTable1.Rows[index]["CodDelito"].ToString());
                    xmlTextWriter1.WriteElementString("Delito", dataTable1.Rows[index]["Delito"].ToString());
                    xmlTextWriter1.WriteElementString("CodigoDelito2", dataTable1.Rows[index]["CodDelito2"].ToString());
                    xmlTextWriter1.WriteElementString("Delito2", dataTable1.Rows[index]["Delito2"].ToString());
                    xmlTextWriter1.WriteElementString("CodigoDelito3", dataTable1.Rows[index]["CodDelito3"].ToString());
                    xmlTextWriter1.WriteElementString("Delito3", dataTable1.Rows[index]["Delito3"].ToString());
                    xmlTextWriter1.WriteElementString("FechaInicioSancion", dataTable1.Rows[index]["FechaInicioSancion"].ToString());
                    xmlTextWriter1.WriteElementString("DuracionSancionAños", dataTable1.Rows[index]["AnosDuracionSancion"].ToString());
                    xmlTextWriter1.WriteElementString("DuracionSancionMeses", dataTable1.Rows[index]["MesesDuracionSancion"].ToString());
                    xmlTextWriter1.WriteElementString("DuracionSancionDias", dataTable1.Rows[index]["DiasDuracionSancion"].ToString());
                    xmlTextWriter1.WriteElementString("DiasAbono", dataTable1.Rows[index]["Abono"].ToString());
                    xmlTextWriter1.WriteElementString("SancionAccesoria", str5);
                    xmlTextWriter1.WriteElementString("FechaTerminoSancionCalculada", dataTable1.Rows[index]["FechaTerminoSancionCalculada"].ToString());
                    xmlTextWriter1.WriteElementString("FechaTerminoSancion", dataTable1.Rows[index]["FechaTerminoSancion"].ToString().Substring(0, 10));
                    xmlTextWriter1.WriteElementString("FechaEgreso", dataTable1.Rows[index]["FechaEgreso"].ToString());
                    xmlTextWriter1.WriteElementString("CausalEgreso", dataTable1.Rows[index]["CausalEgreso"].ToString());
                    xmlTextWriter1.WriteEndElement();
                }
                xmlTextWriter1.WriteEndElement();
            }
            if (dataTable2.Rows.Count > 0)
            {
                xmlTextWriter1.WriteStartElement("HistoricoIngresosPROTECCION");
                for (int index = 0; index < dataTable2.Rows.Count; ++index)
                {
                    xmlTextWriter1.WriteStartElement("HistoricoIngresosTO");
                    xmlTextWriter1.WriteElementString("CodigoIngreso", dataTable2.Rows[index]["ICodIE"].ToString());
                    xmlTextWriter1.WriteElementString("FechaIngreso", dataTable2.Rows[index]["FechaIngreso"].ToString().Substring(0, 10));
                    xmlTextWriter1.WriteElementString("CodigoProyecto", dataTable2.Rows[index]["CodProyecto"].ToString());
                    xmlTextWriter1.WriteElementString("Proyecto", dataTable2.Rows[index]["NombreProyecto"].ToString().Trim());
                    xmlTextWriter1.WriteElementString("ModeloIntervencion", dataTable2.Rows[index]["ModeloIntervencion"].ToString());
                    xmlTextWriter1.WriteElementString("Tribunal", dataTable2.Rows[index]["Tribunal"].ToString());
                    xmlTextWriter1.WriteElementString("RolUnicoCausa", dataTable2.Rows[index]["Ruc"].ToString());
                    xmlTextWriter1.WriteElementString("RolInternoTribunal", dataTable2.Rows[index]["Rit"].ToString());
                    xmlTextWriter1.WriteElementString("CausalIngreso1", dataTable2.Rows[index]["CausalIngreso"].ToString());
                    xmlTextWriter1.WriteElementString("EntidadAsigna1", dataTable2.Rows[index]["EntidadAsigna"].ToString());
                    xmlTextWriter1.WriteElementString("CausalIngreso2", dataTable2.Rows[index]["CausalIngreso2"].ToString());
                    xmlTextWriter1.WriteElementString("EntidadAsigna2", dataTable2.Rows[index]["EntidadAsigna2"].ToString());
                    xmlTextWriter1.WriteElementString("CausalIngreso3", dataTable2.Rows[index]["CausalIngreso3"].ToString());
                    xmlTextWriter1.WriteElementString("EntidadAsigna3", dataTable2.Rows[index]["EntidadAsigna3"].ToString());
                    xmlTextWriter1.WriteElementString("FechaEgreso", dataTable2.Rows[index]["FechaEgreso"].ToString());
                    xmlTextWriter1.WriteElementString("CausalEgreso", dataTable2.Rows[index]["CausalEgreso"].ToString());
                    xmlTextWriter1.WriteElementString("ConQuienEgresa", dataTable2.Rows[index]["ConQuienEgresa"].ToString());
                    xmlTextWriter1.WriteEndElement();
                }
                xmlTextWriter1.WriteEndElement();
            }
            xmlTextWriter1.WriteEndElement();
            xmlTextWriter1.Flush();
            xmlTextWriter1.Close();
            xmlDocument.Load(str2);
            File.Delete(str2);
            return xmlDocument;
        }

        private XmlDocument GetNinoFichaIndividual_TEST(SqlConnection objconn, DataTable dtNino)
        {
            XmlDocument xmlDocument = new XmlDocument();
            string str1 = Guid.NewGuid().ToString();
            if (!Directory.Exists(ConfigurationManager.AppSettings["PathXML"]))
                Directory.CreateDirectory(ConfigurationManager.AppSettings["PathXML"]);
            string str2 = ConfigurationManager.AppSettings["PathXML"] + "FichaIndividual-2012-v1-1" + str1 + ".xml";
            XmlTextWriter xmlTextWriter1 = new XmlTextWriter(str2, Encoding.UTF8);
            xmlTextWriter1.Formatting = Formatting.Indented;
            xmlTextWriter1.WriteStartDocument();
            xmlTextWriter1.WriteStartElement("FichaIndividual");
            string empty = string.Empty;
            string str3 = dtNino.Rows.Count <= 0 ? "RUN NO ENCONTRADO" : "RUN ENCONTRADO";
            xmlTextWriter1.WriteStartElement("Estado");
            xmlTextWriter1.WriteElementString("Estado", str3);
            XmlTextWriter xmlTextWriter2 = xmlTextWriter1;
            string localName = "Fecha";
            DateTime dateTime = DateTime.Now;
            dateTime = dateTime.Date;
            string str4 = dateTime.ToString("dd-MM-yyyy");
            xmlTextWriter2.WriteElementString(localName, str4);
            xmlTextWriter1.WriteEndElement();
            if (dtNino.Rows.Count == 0)
            {
                xmlTextWriter1.WriteEndElement();
                xmlTextWriter1.Flush();
                xmlTextWriter1.Close();
                xmlDocument.Load(str2);
                File.Delete(str2);
                return xmlDocument;
            }
            xmlTextWriter1.WriteStartElement("NinoNinaAdolescente");
            xmlTextWriter1.WriteElementString("CodNino", dtNino.Rows[0]["CodNino"].ToString());
            xmlTextWriter1.WriteStartElement("Run");
            xmlTextWriter1.WriteElementString("numero", dtNino.Rows[0]["Rut"].ToString().Substring(0, 8));
            xmlTextWriter1.WriteElementString("dv", dtNino.Rows[0]["Rut"].ToString().Substring(9, 1));
            xmlTextWriter1.WriteEndElement();
            xmlTextWriter1.WriteStartElement("Nombre");
            xmlTextWriter1.WriteElementString("nombres", dtNino.Rows[0]["Nombres"].ToString());
            xmlTextWriter1.WriteElementString("apellidoPaterno", dtNino.Rows[0]["Apellido_Paterno"].ToString());
            xmlTextWriter1.WriteElementString("apellidoMaterno", dtNino.Rows[0]["Apellido_Materno"].ToString());
            xmlTextWriter1.WriteEndElement();
            xmlTextWriter1.WriteElementString("FechaNacimiento", dtNino.Rows[0]["FechaNacimiento"].ToString());
            xmlTextWriter1.WriteElementString("Edad", dtNino.Rows[0]["Edad"].ToString());
            xmlTextWriter1.WriteElementString("Sexo", dtNino.Rows[0]["Sexo"].ToString());
            xmlTextWriter1.WriteElementString("CodNacionalidad", dtNino.Rows[0]["CodNacionalidad"].ToString());
            xmlTextWriter1.WriteElementString("Nacionalidad", dtNino.Rows[0]["Nacionalidad"].ToString());
            DataTable dataTable1 = this.CargaDatosIngreso(dtNino.Rows[0]["CodNino"].ToString());
            if (dataTable1.Rows.Count == 0)
            {
                xmlTextWriter1.WriteEndElement();
                xmlTextWriter1.Flush();
                xmlTextWriter1.Close();
                xmlDocument.Load(str2);
                File.Delete(str2);
                return xmlDocument;
            }
            string ICodIE = dataTable1.Rows[0]["ICodIE"].ToString();
            DataTable dataTable2 = this.CargaPadres(ICodIE);
            DataTable dataTable3 = this.CargaDatosDiscapacidad(ICodIE);
            DataTable dataTable4 = this.CargaDatosEscolaridad(ICodIE);
            DataTable dataTable5 = this.CargaDatosEscolaridad(ICodIE);
            DataTable dataTable6 = this.CargaDatosDrogas(ICodIE);
            DataTable dataTable7 = this.CargaDatosPlanIntervencion(ICodIE);
            xmlTextWriter1.WriteStartElement("Padres");
            if (dataTable2.Rows.Count > 0)
            {
                xmlTextWriter1.WriteStartElement("Padre");
                xmlTextWriter1.WriteStartElement("Run");
                if (dataTable2.Rows[0]["RutPadre"].ToString() != "" && dataTable2.Rows[0]["RutPadre"].ToString().Length > 3)
                {
                    string str5 = dataTable2.Rows[0]["RutPadre"].ToString();
                    xmlTextWriter1.WriteElementString("numero", str5.Substring(0, str5.Length - 2));
                    xmlTextWriter1.WriteElementString("dv", str5.Substring(str5.Length - 1, 1));
                }
                else
                {
                    xmlTextWriter1.WriteElementString("numero", "");
                    xmlTextWriter1.WriteElementString("dv", "");
                }
                xmlTextWriter1.WriteEndElement();
                xmlTextWriter1.WriteStartElement("Nombre");
                if (dataTable2.Rows[0]["NombrePadre"].ToString() != "")
                {
                    xmlTextWriter1.WriteElementString("nombres", dataTable2.Rows[0]["NombrePadre"].ToString());
                    xmlTextWriter1.WriteElementString("apellidoPaterno", dataTable2.Rows[0]["ApellidoPaternoPadre"].ToString());
                    xmlTextWriter1.WriteElementString("apellidoMaterno", dataTable2.Rows[0]["ApellidoMaternoPadre"].ToString());
                }
                else
                {
                    xmlTextWriter1.WriteElementString("nombres", "NO REGISTRA INFORMACIÓN");
                    xmlTextWriter1.WriteElementString("apellidoPaterno", "NO REGISTRA INFORMACIÓN");
                    xmlTextWriter1.WriteElementString("apellidoMaterno", "NO REGISTRA INFORMACIÓN");
                }
                xmlTextWriter1.WriteEndElement();
                xmlTextWriter1.WriteEndElement();
                xmlTextWriter1.WriteStartElement("Madre");
                xmlTextWriter1.WriteStartElement("Run");
                if (dataTable2.Rows[0]["RutMadre"].ToString() != "" && dataTable2.Rows[0]["RutMadre"].ToString().Length > 3)
                {
                    string str5 = dataTable2.Rows[0]["RutMadre"].ToString();
                    xmlTextWriter1.WriteElementString("numero", str5.Substring(0, str5.Length - 2));
                    xmlTextWriter1.WriteElementString("dv", str5.Substring(str5.Length - 1, 1));
                }
                else
                {
                    xmlTextWriter1.WriteElementString("numero", "");
                    xmlTextWriter1.WriteElementString("dv", "");
                }
                xmlTextWriter1.WriteEndElement();
                xmlTextWriter1.WriteStartElement("Nombre");
                if (dataTable2.Rows[0]["NombreMadre"].ToString() != "")
                {
                    xmlTextWriter1.WriteElementString("nombres", dataTable2.Rows[0]["NombreMadre"].ToString());
                    xmlTextWriter1.WriteElementString("apellidoPaterno", dataTable2.Rows[0]["ApellidoPaternoMadre"].ToString());
                    xmlTextWriter1.WriteElementString("apellidoMaterno", dataTable2.Rows[0]["ApellidoMaternoMadre"].ToString());
                }
                else
                {
                    xmlTextWriter1.WriteElementString("nombres", "NO REGISTRA INFORMACIÓN");
                    xmlTextWriter1.WriteElementString("apellidoPaterno", "NO REGISTRA INFORMACIÓN");
                    xmlTextWriter1.WriteElementString("apellidoMaterno", "NO REGISTRA INFORMACIÓN");
                }
                xmlTextWriter1.WriteEndElement();
                xmlTextWriter1.WriteEndElement();
            }
            else
            {
                xmlTextWriter1.WriteStartElement("Padre");
                xmlTextWriter1.WriteStartElement("Run");
                xmlTextWriter1.WriteElementString("numero", "");
                xmlTextWriter1.WriteElementString("dv", "");
                xmlTextWriter1.WriteEndElement();
                xmlTextWriter1.WriteStartElement("Nombre");
                xmlTextWriter1.WriteElementString("nombres", "NO REGISTRA INFORMACIÓN");
                xmlTextWriter1.WriteElementString("apellidoPaterno", "NO REGISTRA INFORMACIÓN");
                xmlTextWriter1.WriteElementString("apellidoMaterno", "NO REGISTRA INFORMACIÓN");
                xmlTextWriter1.WriteEndElement();
                xmlTextWriter1.WriteEndElement();
                xmlTextWriter1.WriteStartElement("Madre");
                xmlTextWriter1.WriteStartElement("Run");
                xmlTextWriter1.WriteElementString("numero", "");
                xmlTextWriter1.WriteElementString("dv", "");
                xmlTextWriter1.WriteEndElement();
                xmlTextWriter1.WriteStartElement("Nombre");
                xmlTextWriter1.WriteElementString("nombres", "NO REGISTRA INFORMACIÓN");
                xmlTextWriter1.WriteElementString("apellidoPaterno", "NO REGISTRA INFORMACIÓN");
                xmlTextWriter1.WriteElementString("apellidoMaterno", "NO REGISTRA INFORMACIÓN");
                xmlTextWriter1.WriteEndElement();
                xmlTextWriter1.WriteEndElement();
            }
            xmlTextWriter1.WriteEndElement();
            xmlTextWriter1.WriteStartElement("HermanosEnElSistema");
            DataTable dataTable8 = this.CargaHermanosEnElSistema(dtNino.Rows[0]["CodNino"].ToString());
            if (dataTable8.Rows.Count > 0)
            {
                for (int index = 0; index < dataTable8.Rows.Count; ++index)
                {
                    xmlTextWriter1.WriteStartElement("Nombre");
                    xmlTextWriter1.WriteElementString("nombres", dataTable8.Rows[index]["Nombres"].ToString());
                    xmlTextWriter1.WriteElementString("apellidoPaterno", dataTable8.Rows[index]["Apellido_Paterno"].ToString());
                    xmlTextWriter1.WriteElementString("apellidoMaterno", dataTable8.Rows[index]["Apellido_Materno"].ToString());
                    xmlTextWriter1.WriteEndElement();
                }
            }
            else
            {
                xmlTextWriter1.WriteStartElement("Nombre");
                xmlTextWriter1.WriteElementString("nombres", "NO REGISTRA INFORMACIÓN");
                xmlTextWriter1.WriteElementString("apellidoPaterno", "NO REGISTRA INFORMACIÓN");
                xmlTextWriter1.WriteElementString("apellidoMaterno", "NO REGISTRA INFORMACIÓN");
                xmlTextWriter1.WriteEndElement();
            }
            xmlTextWriter1.WriteEndElement();
            xmlTextWriter1.WriteStartElement("Salud");
            if (dataTable3.Rows.Count > 0)
            {
                for (int index = 0; index < dataTable3.Rows.Count; ++index)
                {
                    xmlTextWriter1.WriteStartElement("Discapacidad");
                    xmlTextWriter1.WriteElementString("CodTipoDiscapacidad", dataTable3.Rows[index]["CodTipoDiscapacidad"].ToString());
                    xmlTextWriter1.WriteElementString("Discapacidad", dataTable3.Rows[index]["Discapacidad"].ToString());
                    xmlTextWriter1.WriteElementString("CodNivelDiscapacidad", dataTable3.Rows[index]["CodNivelDiscapacidad"].ToString());
                    xmlTextWriter1.WriteElementString("NivelDiscapacidad", dataTable3.Rows[index]["NivelDiscapacidad"].ToString());
                    xmlTextWriter1.WriteEndElement();
                }
            }
            else
            {
                xmlTextWriter1.WriteStartElement("Discapacidad");
                xmlTextWriter1.WriteElementString("CodTipoDiscapacidad", "NO REGISTRA INFORMACIÓN");
                xmlTextWriter1.WriteElementString("Discapacidad", "");
                xmlTextWriter1.WriteElementString("CodNivelDiscapacidad", "");
                xmlTextWriter1.WriteElementString("NivelDiscapacidad", "NO REGISTRA INFORMACIÓN");
                xmlTextWriter1.WriteEndElement();
            }
            if (dataTable4.Rows.Count > 0)
            {
                for (int index = 0; index < dataTable4.Rows.Count; ++index)
                {
                    xmlTextWriter1.WriteStartElement("EnfermedadCronica");
                    xmlTextWriter1.WriteElementString("CodEnfermedadCronica", dataTable4.Rows[index]["CodEnfermedadCronica"].ToString());
                    xmlTextWriter1.WriteElementString("EnfermedadCronica", dataTable4.Rows[index]["EnfermedadCronica"].ToString());
                    xmlTextWriter1.WriteEndElement();
                }
            }
            else
            {
                xmlTextWriter1.WriteStartElement("EnfermedadCronica");
                xmlTextWriter1.WriteElementString("CodEnfermedadCronica", "");
                xmlTextWriter1.WriteElementString("EnfermedadCronica", "NO REGISTRA INFORMACIÓN");
                xmlTextWriter1.WriteEndElement();
            }
            xmlTextWriter1.WriteEndElement();
            xmlTextWriter1.WriteStartElement("Escolaridad");
            if (dataTable5.Rows.Count > 0)
            {
                xmlTextWriter1.WriteElementString("CodEscolaridad", dataTable5.Rows[0]["CodEscolaridad"].ToString());
                xmlTextWriter1.WriteElementString("Escolaridad", dataTable5.Rows[0]["Escolaridad"].ToString());
                xmlTextWriter1.WriteElementString("CodTipoAsistenciaEscolar", dataTable5.Rows[0]["CodTipoAsistenciaEscolar"].ToString());
                xmlTextWriter1.WriteElementString("TipoAsistenciaEscolar", dataTable5.Rows[0]["TipoAsistenciaEscolar"].ToString());
                xmlTextWriter1.WriteElementString("AgnoEscolar", dataTable5.Rows[0]["AnoUltimoCursoAprobado"].ToString());
            }
            else
            {
                xmlTextWriter1.WriteElementString("CodEscolaridad", "");
                xmlTextWriter1.WriteElementString("Escolaridad", "NO REGISTRA INFORMACIÓN");
                xmlTextWriter1.WriteElementString("CodTipoAsistenciaEscolar", "");
                xmlTextWriter1.WriteElementString("TipoAsistenciaEscolar", "NO REGISTRA INFORMACIÓN");
                xmlTextWriter1.WriteElementString("AgnoEscolar", "");
            }
            xmlTextWriter1.WriteEndElement();
            xmlTextWriter1.WriteStartElement("Drogas");
            if (dataTable6.Rows.Count > 0)
            {
                xmlTextWriter1.WriteElementString("CodDroga", dataTable6.Rows[0]["CodDroga"].ToString());
                xmlTextWriter1.WriteElementString("Droga", dataTable6.Rows[0]["Droga"].ToString());
                xmlTextWriter1.WriteElementString("CodTipoConsumoDroga", dataTable6.Rows[0]["CodTipoConsumoDroga"].ToString());
                xmlTextWriter1.WriteElementString("TipoConsumoDroga", dataTable6.Rows[0]["TipoConsumoDroga"].ToString());
            }
            else
            {
                xmlTextWriter1.WriteElementString("CodDroga", "");
                xmlTextWriter1.WriteElementString("Droga", "NO REGISTRA INFORMACIÓN");
                xmlTextWriter1.WriteElementString("CodTipoConsumoDroga", "");
                xmlTextWriter1.WriteElementString("TipoConsumoDroga", "NO REGISTRA INFORMACIÓN");
            }
            xmlTextWriter1.WriteEndElement();
            xmlTextWriter1.WriteStartElement("PlanIntervencion");
            if (dataTable7.Rows.Count > 0)
            {
                xmlTextWriter1.WriteElementString("FechaInicioPlanIntervencion", dataTable7.Rows[0]["FechaInicioPlanIntervencion"].ToString());
                xmlTextWriter1.WriteElementString("FechaTerminoEstimadaPlanIntervencion", dataTable7.Rows[0]["FechaTerminoEstimadaPlanIntervencion"].ToString());
            }
            else
            {
                xmlTextWriter1.WriteElementString("FechaInicioPlanIntervencion", "");
                xmlTextWriter1.WriteElementString("FechaTerminoEstimadaPlanIntervencion", "");
            }
            xmlTextWriter1.WriteEndElement();
            xmlTextWriter1.WriteStartElement("Comuna");
            if (dataTable1.Rows.Count > 0 && dataTable1.Rows[0]["CodigoComunaNino"].ToString() != "")
            {
                xmlTextWriter1.WriteElementString("CodigoComuna", dataTable1.Rows[0]["CodigoComunaNino"].ToString());
                xmlTextWriter1.WriteElementString("DescripcionComuna", dataTable1.Rows[0]["ComunaNino"].ToString());
            }
            else
            {
                xmlTextWriter1.WriteElementString("CodigoComuna", "");
                xmlTextWriter1.WriteElementString("DescripcionComuna", "NO REGISTRA INFORMACIÓN");
            }
            xmlTextWriter1.WriteEndElement();
            xmlTextWriter1.WriteEndElement();
            xmlTextWriter1.WriteStartElement("Ingreso");
            xmlTextWriter1.WriteElementString("FechaIngreso", dataTable1.Rows[0]["FechaIngreso"].ToString());
            xmlTextWriter1.WriteElementString("FechaIngresoAlSistema", dataTable1.Rows[0]["FechaIngresoAlSistema"].ToString());
            xmlTextWriter1.WriteStartElement("SolicitanteIngreso");
            xmlTextWriter1.WriteElementString("CodSolicitanteIngreso", dataTable1.Rows[0]["CodSolicitanteIngreso"].ToString());
            xmlTextWriter1.WriteElementString("SolicitanteIngreso", dataTable1.Rows[0]["SolicitanteIngreso"].ToString());
            xmlTextWriter1.WriteEndElement();
            xmlTextWriter1.WriteStartElement("Proyecto");
            xmlTextWriter1.WriteElementString("CodProyecto", dataTable1.Rows[0]["CodProyecto"].ToString());
            xmlTextWriter1.WriteElementString("Proyecto", dataTable1.Rows[0]["Proyecto"].ToString());
            xmlTextWriter1.WriteStartElement("Comuna");
            if (dataTable1.Rows.Count > 0 && dataTable1.Rows[0]["CodigoComunaProyecto"].ToString() != "")
            {
                xmlTextWriter1.WriteElementString("CodigoComuna", dataTable1.Rows[0]["CodigoComunaProyecto"].ToString());
                xmlTextWriter1.WriteElementString("DescripcionComuna", dataTable1.Rows[0]["ComunaProyecto"].ToString());
            }
            else
            {
                xmlTextWriter1.WriteElementString("CodigoComuna", "0");
                xmlTextWriter1.WriteElementString("DescripcionComuna", "NO REGISTRA INFORMACIÓN");
            }
            xmlTextWriter1.WriteEndElement();
            xmlTextWriter1.WriteEndElement();
            xmlTextWriter1.WriteEndElement();
            xmlTextWriter1.WriteStartElement("Parametricas");
            xmlTextWriter1.WriteStartElement("parProyectos");
            DataTable dataTable9 = this.CargaParametricaProyectos();
            for (int index = 0; index < dataTable9.Rows.Count; ++index)
            {
                xmlTextWriter1.WriteElementString("CodigoProyecto", dataTable9.Rows[index]["CodigoProyecto"].ToString());
                xmlTextWriter1.WriteElementString("Proyecto", dataTable9.Rows[index]["Proyecto"].ToString());
            }
            xmlTextWriter1.WriteEndElement();
            xmlTextWriter1.WriteStartElement("parSolicitanteIngreso");
            DataTable dataTable10 = this.CargaParametricaSolicitanteIngreso();
            for (int index = 0; index < dataTable10.Rows.Count; ++index)
            {
                xmlTextWriter1.WriteElementString("CodSolicitanteIngreso", dataTable10.Rows[index]["CodSolicitanteIngreso"].ToString());
                xmlTextWriter1.WriteElementString("SolicitanteIngreso", dataTable10.Rows[index]["SolicitanteIngreso"].ToString());
            }
            xmlTextWriter1.WriteEndElement();
            xmlTextWriter1.WriteStartElement("parTipoDiscapacidad");
            DataTable dataTable11 = this.CargaParametricaTipoDiscapacidad();
            for (int index = 0; index < dataTable11.Rows.Count; ++index)
            {
                xmlTextWriter1.WriteElementString("CodTipoDiscapacidad", dataTable11.Rows[index]["CodTipoDiscapacidad"].ToString());
                xmlTextWriter1.WriteElementString("TipoDiscapacidad", dataTable11.Rows[index]["TipoDiscapacidad"].ToString());
            }
            xmlTextWriter1.WriteEndElement();
            xmlTextWriter1.WriteStartElement("parNivelDiscapacidad");
            DataTable dataTable12 = this.CargaParametricaNivelDiscapacidad();
            for (int index = 0; index < dataTable12.Rows.Count; ++index)
            {
                xmlTextWriter1.WriteElementString("CodNivelDiscapacidad", dataTable12.Rows[index]["CodNivelDiscapacidad"].ToString());
                xmlTextWriter1.WriteElementString("NivelDiscapacidad", dataTable12.Rows[index]["NivelDiscapacidad"].ToString());
            }
            xmlTextWriter1.WriteEndElement();
            xmlTextWriter1.WriteStartElement("parEnfermedadesCronicas");
            DataTable dataTable13 = this.CargaParametricaEnfermedadesCronicas();
            for (int index = 0; index < dataTable13.Rows.Count; ++index)
            {
                xmlTextWriter1.WriteElementString("CodEnfermedadCronica", dataTable13.Rows[index]["CodEnfermedadCronica"].ToString());
                xmlTextWriter1.WriteElementString("EnfermedadCronica", dataTable13.Rows[index]["EnfermedadCronica"].ToString());
            }
            xmlTextWriter1.WriteEndElement();
            xmlTextWriter1.WriteStartElement("parEscolaridad");
            DataTable dataTable14 = this.CargaParametricaEscolaridad();
            for (int index = 0; index < dataTable14.Rows.Count; ++index)
            {
                xmlTextWriter1.WriteElementString("CodEscolaridad", dataTable14.Rows[index]["CodEscolaridad"].ToString());
                xmlTextWriter1.WriteElementString("Escolaridad", dataTable14.Rows[index]["Escolaridad"].ToString());
            }
            xmlTextWriter1.WriteEndElement();
            xmlTextWriter1.WriteStartElement("parTipoAsistenciaEscolar");
            DataTable dataTable15 = this.CargaParametricaTipoAsistenciaEscolar();
            for (int index = 0; index < dataTable15.Rows.Count; ++index)
            {
                xmlTextWriter1.WriteElementString("CodTipoAsistenciaEscolar", dataTable15.Rows[index]["CodTipoAsistenciaEscolar"].ToString());
                xmlTextWriter1.WriteElementString("TipoAsistenciaEscolar", dataTable15.Rows[index]["TipoAsistenciaEscolar"].ToString());
            }
            xmlTextWriter1.WriteEndElement();
            xmlTextWriter1.WriteStartElement("parDrogas");
            DataTable dataTable16 = this.CargaParametricaDrogas();
            for (int index = 0; index < dataTable16.Rows.Count; ++index)
            {
                xmlTextWriter1.WriteElementString("CodDroga", dataTable16.Rows[index]["CodDroga"].ToString());
                xmlTextWriter1.WriteElementString("Droga", dataTable16.Rows[index]["Droga"].ToString());
            }
            xmlTextWriter1.WriteEndElement();
            xmlTextWriter1.WriteStartElement("parTipoConsumoDroga");
            DataTable dataTable17 = this.CargaParametricaTipoConsumoDroga();
            for (int index = 0; index < dataTable17.Rows.Count; ++index)
            {
                xmlTextWriter1.WriteElementString("CodTipoConsumoDroga", dataTable17.Rows[index]["CodTipoConsumoDroga"].ToString());
                xmlTextWriter1.WriteElementString("TipoConsumoDroga", dataTable17.Rows[index]["TipoConsumoDroga"].ToString());
            }
            xmlTextWriter1.WriteEndElement();
            xmlTextWriter1.WriteStartElement("parNacionalidades");
            DataTable dataTable18 = this.CargaParametricaNacionalidad();
            for (int index = 0; index < dataTable18.Rows.Count; ++index)
            {
                xmlTextWriter1.WriteElementString("CodNacionalidad", dataTable18.Rows[index]["CodNacionalidad"].ToString());
                xmlTextWriter1.WriteElementString("Nacionalidad", dataTable18.Rows[index]["Nacionalidad"].ToString());
            }
            xmlTextWriter1.WriteEndElement();
            xmlTextWriter1.WriteEndElement();
            xmlTextWriter1.Flush();
            xmlTextWriter1.Close();
            xmlDocument.Load(str2);
            File.Delete(str2);
            return xmlDocument;
        }

        

        private XmlDocument GetCatastroJusticiaJuvenil(DataTable dc)
        {
            XmlDocument xmlDocument = new XmlDocument();
            string str1 = Guid.NewGuid().ToString();
            if (!Directory.Exists(ConfigurationManager.AppSettings["PathXML"].ToString()))
                Directory.CreateDirectory(ConfigurationManager.AppSettings["PathXML"].ToString());
            string str2 = ConfigurationManager.AppSettings["PathXML"].ToString() + "DJJ-" + str1 + ".xml";
            XmlTextWriter xmlTextWriter = new XmlTextWriter(str2, Encoding.UTF8);
            xmlTextWriter.Formatting = Formatting.Indented;
            xmlTextWriter.WriteStartDocument();
            xmlTextWriter.WriteStartElement("DJJ");
            if (dc.Rows.Count > 0)
            {
                for (int index = 0; index < dc.Rows.Count; ++index)
                {
                    xmlTextWriter.WriteStartElement("Proyecto");
                    xmlTextWriter.WriteElementString("CodigoRegion", dc.Rows[index]["CodigoRegion"].ToString());
                    xmlTextWriter.WriteElementString("nCodRegion", dc.Rows[index]["nCodRegion"].ToString());
                    xmlTextWriter.WriteElementString("CodigoComuna", dc.Rows[index]["CodigoComuna"].ToString());
                    xmlTextWriter.WriteElementString("nComuna", dc.Rows[index]["nComuna"].ToString());
                    xmlTextWriter.WriteElementString("CodProvincia", dc.Rows[index]["CodProvincia"].ToString());
                    xmlTextWriter.WriteElementString("nCodProvincia", dc.Rows[index]["nCodProvincia"].ToString());
                    xmlTextWriter.WriteElementString("CodProyecto", dc.Rows[index]["CodProyecto"].ToString());
                    xmlTextWriter.WriteElementString("Nombre", dc.Rows[index]["Nombre"].ToString());
                    xmlTextWriter.WriteElementString("Direccion", dc.Rows[index]["Direccion"].ToString());
                    xmlTextWriter.WriteElementString("Telefono", dc.Rows[index]["Telefono"].ToString());
                    xmlTextWriter.WriteElementString("Mail", dc.Rows[index]["Mail"].ToString());
                    xmlTextWriter.WriteElementString("Fax", dc.Rows[index]["Fax"].ToString());
                    xmlTextWriter.WriteElementString("Director", dc.Rows[index]["Director"].ToString());
                    xmlTextWriter.WriteElementString("TipoProyecto", dc.Rows[index]["TipoProyecto"].ToString());
                    xmlTextWriter.WriteElementString("nTipoProyecto", dc.Rows[index]["nTipoProyecto"].ToString());
                    xmlTextWriter.WriteElementString("TipoSubvencion", dc.Rows[index]["TipoSubvencion"].ToString());
                    xmlTextWriter.WriteElementString("nTipoSubvencion", dc.Rows[index]["nTipoSubvencion"].ToString());
                    xmlTextWriter.WriteElementString("CodTematicaProyecto", dc.Rows[index]["CodTematicaProyecto"].ToString());
                    xmlTextWriter.WriteElementString("nCodTematica", dc.Rows[index]["nCodTematica"].ToString());
                    xmlTextWriter.WriteElementString("CodModeloIntervencion", dc.Rows[index]["CodModeloIntervencion"].ToString());
                    xmlTextWriter.WriteElementString("nModelo", dc.Rows[index]["nModelo"].ToString());
                    xmlTextWriter.WriteElementString("NumeroPlazas", dc.Rows[index]["NumeroPlazas"].ToString());
                    xmlTextWriter.WriteElementString("SexoPorConvenio", dc.Rows[index]["Sexo"].ToString());
                    xmlTextWriter.WriteElementString("CodDiasAtencion", dc.Rows[index]["CodDiasAtencion"].ToString());
                    xmlTextWriter.WriteElementString("IndVigencia", dc.Rows[index]["IndVigencia"].ToString());
                    xmlTextWriter.WriteElementString("FechaInicio", dc.Rows[index]["FechaInicio"].ToString());
                    xmlTextWriter.WriteElementString("FechaTermino", dc.Rows[index]["FechaTermino"].ToString());
                    xmlTextWriter.WriteElementString("NemoTecnico", dc.Rows[index]["NemoTecnico"].ToString());
                    xmlTextWriter.WriteElementString("CodDepartamentosSename", dc.Rows[index]["CodDepartamentosSename"].ToString());
                    xmlTextWriter.WriteElementString("FechaCreacion", dc.Rows[index]["FechaCreacion"].ToString());
                    xmlTextWriter.WriteElementString("Tematica", dc.Rows[index]["Tematica"].ToString());
                    xmlTextWriter.WriteElementString("Peso", dc.Rows[index]["Peso"].ToString());
                    xmlTextWriter.WriteElementString("VigenciaTematica", dc.Rows[index]["VigenciaTematica"].ToString());
                    xmlTextWriter.WriteElementString("FechaConvenio", dc.Rows[index]["FechaConvenio"].ToString());
                    xmlTextWriter.WriteElementString("FechaConvenio2", dc.Rows[index]["FechaConvenio2"].ToString());
                    xmlTextWriter.WriteElementString("CodInstitucion", dc.Rows[index]["CodInstitucion"].ToString());
                    xmlTextWriter.WriteElementString("NombreInstitucion", dc.Rows[index]["NombreInstitucion"].ToString());
                    xmlTextWriter.WriteElementString("CodSistemaAsistencial", dc.Rows[index]["CodSistemaAsistencial"].ToString());
                    xmlTextWriter.WriteElementString("NombreSistemaAsistencial", dc.Rows[index]["NombreSistemaAsistencial"].ToString());
                    xmlTextWriter.WriteElementString("NombreDepartamentosSename", dc.Rows[index]["NombreDepartamentosSename"].ToString());
                    xmlTextWriter.WriteElementString("EdadMinima", dc.Rows[index]["EdadMinima"].ToString());
                    xmlTextWriter.WriteElementString("EdadMaxima", dc.Rows[index]["EdadMaxima"].ToString());
                    xmlTextWriter.WriteElementString("FechaVigenciaMes", dc.Rows[index]["FechaVigenciaMes"].ToString());
                    xmlTextWriter.WriteElementString("FechaVigenciaAgno", dc.Rows[index]["FechaVigenciaAgno"].ToString());
                    xmlTextWriter.WriteElementString("NomRegion", dc.Rows[index]["NomRegion"].ToString());
                    xmlTextWriter.WriteElementString("Monto", dc.Rows[index]["Monto"].ToString());
                    xmlTextWriter.WriteElementString("Criterio", dc.Rows[index]["Criterio"].ToString());
                    xmlTextWriter.WriteElementString("Pagina", dc.Rows[index]["Pagina"].ToString());
                    xmlTextWriter.WriteElementString("Link", dc.Rows[index]["Link"].ToString());
                    xmlTextWriter.WriteElementString("PlazasPlazasOcupadas", dc.Rows[index]["PlazasOcupadas"].ToString());
                    xmlTextWriter.WriteEndElement();
                }
            }
            xmlTextWriter.WriteEndElement();
            xmlTextWriter.Flush();
            xmlTextWriter.Close();
            xmlDocument.Load(str2);
            File.Delete(str2);
            return xmlDocument;
        }

        private XmlDocument GetCatastroJuecesSenameProtecion(DataTable db)
        {
            XmlDocument xmlDocument = new XmlDocument();
            string str1 = Guid.NewGuid().ToString();
            if (!Directory.Exists(ConfigurationManager.AppSettings["PathXML"].ToString()))
                Directory.CreateDirectory(ConfigurationManager.AppSettings["PathXML"].ToString());
            string str2 = ConfigurationManager.AppSettings["PathXML"].ToString() + "DEPRODE-" + str1 + ".xml";
            XmlTextWriter xmlTextWriter = new XmlTextWriter(str2, Encoding.UTF8);
            xmlTextWriter.Formatting = Formatting.Indented;
            xmlTextWriter.WriteStartDocument();
            xmlTextWriter.WriteStartElement("DEPRODE");
            if (db.Rows.Count > 0)
            {
                for (int index = 0; index < db.Rows.Count; ++index)
                {
                    xmlTextWriter.WriteStartElement("Proyecto");
                    xmlTextWriter.WriteElementString("CodigoRegion", db.Rows[index]["CodigoRegion"].ToString());
                    xmlTextWriter.WriteElementString("nCodRegion", db.Rows[index]["nCodRegion"].ToString());
                    xmlTextWriter.WriteElementString("CodigoComuna", db.Rows[index]["CodigoComuna"].ToString());
                    xmlTextWriter.WriteElementString("nComuna", db.Rows[index]["nComuna"].ToString());
                    xmlTextWriter.WriteElementString("CodProvincia", db.Rows[index]["CodProvincia"].ToString());
                    xmlTextWriter.WriteElementString("nCodProvincia", db.Rows[index]["nCodProvincia"].ToString());
                    xmlTextWriter.WriteElementString("CodProyecto", db.Rows[index]["CodProyecto"].ToString());
                    xmlTextWriter.WriteElementString("Nombre", db.Rows[index]["Nombre"].ToString());
                    xmlTextWriter.WriteElementString("Direccion", db.Rows[index]["Direccion"].ToString());
                    xmlTextWriter.WriteElementString("Telefono", db.Rows[index]["Telefono"].ToString());
                    xmlTextWriter.WriteElementString("Mail", db.Rows[index]["Mail"].ToString());
                    xmlTextWriter.WriteElementString("Fax", db.Rows[index]["Fax"].ToString());
                    xmlTextWriter.WriteElementString("Director", db.Rows[index]["Director"].ToString());
                    xmlTextWriter.WriteElementString("TipoProyecto", db.Rows[index]["TipoProyecto"].ToString());
                    xmlTextWriter.WriteElementString("nTipoProyecto", db.Rows[index]["nTipoProyecto"].ToString());
                    xmlTextWriter.WriteElementString("TipoSubvencion", db.Rows[index]["TipoSubvencion"].ToString());
                    xmlTextWriter.WriteElementString("nTipoSubvencion", db.Rows[index]["nTipoSubvencion"].ToString());
                    xmlTextWriter.WriteElementString("CodTematicaProyecto", db.Rows[index]["CodTematicaProyecto"].ToString());
                    xmlTextWriter.WriteElementString("nCodTematica", db.Rows[index]["nCodTematica"].ToString());
                    xmlTextWriter.WriteElementString("CodModeloIntervencion", db.Rows[index]["CodModeloIntervencion"].ToString());
                    xmlTextWriter.WriteElementString("nModelo", db.Rows[index]["nModelo"].ToString());
                    xmlTextWriter.WriteElementString("NumeroPlazas", db.Rows[index]["NumeroPlazas"].ToString());
                    xmlTextWriter.WriteElementString("SexoPorConvenio", db.Rows[index]["Sexo"].ToString());
                    xmlTextWriter.WriteElementString("CodDiasAtencion", db.Rows[index]["CodDiasAtencion"].ToString());
                    xmlTextWriter.WriteElementString("IndVigencia", db.Rows[index]["IndVigencia"].ToString());
                    xmlTextWriter.WriteElementString("FechaInicio", db.Rows[index]["FechaInicio"].ToString());
                    xmlTextWriter.WriteElementString("FechaTermino", db.Rows[index]["FechaTermino"].ToString());
                    xmlTextWriter.WriteElementString("NemoTecnico", db.Rows[index]["NemoTecnico"].ToString());
                    xmlTextWriter.WriteElementString("CodDepartamentosSename", db.Rows[index]["CodDepartamentosSename"].ToString());
                    xmlTextWriter.WriteElementString("FechaCreacion", db.Rows[index]["FechaCreacion"].ToString());
                    xmlTextWriter.WriteElementString("Tematica", db.Rows[index]["Tematica"].ToString());
                    xmlTextWriter.WriteElementString("Peso", db.Rows[index]["Peso"].ToString());
                    xmlTextWriter.WriteElementString("VigenciaTematica", db.Rows[index]["VigenciaTematica"].ToString());
                    xmlTextWriter.WriteElementString("FechaConvenio", db.Rows[index]["FechaConvenio"].ToString());
                    xmlTextWriter.WriteElementString("FechaConvenio2", db.Rows[index]["FechaConvenio2"].ToString());
                    xmlTextWriter.WriteElementString("CodInstitucion", db.Rows[index]["CodInstitucion"].ToString());
                    xmlTextWriter.WriteElementString("NombreInstitucion", db.Rows[index]["NombreInstitucion"].ToString());
                    xmlTextWriter.WriteElementString("CodSistemaAsistencial", db.Rows[index]["CodSistemaAsistencial"].ToString());
                    xmlTextWriter.WriteElementString("NombreSistemaAsistencial", db.Rows[index]["NombreSistemaAsistencial"].ToString());
                    xmlTextWriter.WriteElementString("NombreDepartamentosSename", db.Rows[index]["NombreDepartamentosSename"].ToString());
                    xmlTextWriter.WriteElementString("EdadMinima", db.Rows[index]["EdadMinima"].ToString());
                    xmlTextWriter.WriteElementString("EdadMaxima", db.Rows[index]["EdadMaxima"].ToString());
                    xmlTextWriter.WriteElementString("FechaVigenciaMes", db.Rows[index]["FechaVigenciaMes"].ToString());
                    xmlTextWriter.WriteElementString("FechaVigenciaAgno", db.Rows[index]["FechaVigenciaAgno"].ToString());
                    xmlTextWriter.WriteElementString("NomRegion", db.Rows[index]["NomRegion"].ToString());
                    xmlTextWriter.WriteElementString("Monto", db.Rows[index]["Monto"].ToString());
                    xmlTextWriter.WriteElementString("Criterio", db.Rows[index]["Criterio"].ToString());
                    xmlTextWriter.WriteElementString("Pagina", db.Rows[index]["Pagina"].ToString());
                    xmlTextWriter.WriteElementString("Link", db.Rows[index]["Link"].ToString());
                    xmlTextWriter.WriteElementString("PlazasOcupadas", db.Rows[index]["PlazasOcupadas"].ToString());
                    xmlTextWriter.WriteEndElement();
                }
            }
            xmlTextWriter.WriteEndElement();
            xmlTextWriter.Flush();
            xmlTextWriter.Close();
            xmlDocument.Load(str2);
            File.Delete(str2);
            return xmlDocument;
        }

        public XmlDataDocument DocXML { get; set; }

        public object DsGetResultado { get; set; }
      

    }
}