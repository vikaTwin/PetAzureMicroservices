using Dapper;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using WisdomPetMedicine.Hospital.Domain.Entities;

namespace WisdomPetMedicine.Hospital.Projector
{
    static class SqlConnectionExtensions
    {
        public static void EnsurePatientsTable(this SqlConnection conn)
        {
            var query = @"
                        IF (NOT EXISTS (SELECT *
                        FROM INFORMATION_SCHEMA.TABLES
                        WHERE TABLE_SCHEMA = 'dbo'
                        AND TABLE_NAME = 'Patients'))
                        CREATE TABLE [dbo].[Patients](
                        [Id] [uniqueidentifier] NOT NULL,
                        [BloodType] [nvarchar](max) NULL,
                        [Weight] [decimal](18, 2) NULL,
                        [Status] [nvarchar](max) NOT NULL,
                        [UpdatedOn] [datetime] NOT NULL
                        CONSTRAINT [PK_Patients] PRIMARY KEY CLUSTERED ([id] ASC))";
            
            conn.Execute(query);
        }

        public static void InsertPatient(this SqlConnection conn, Patient patient)
        {
            conn.Execute(@"DELETE FROM Patients WHERE Id = @Id 
                           INSERT INTO Patients (Id, BloodType, Weight, Status, UpdatedOn) VALUES (@Id, @BloodType, @Weight, @Status, GETUTCDATE())",
                           new { Id = patient.Id, BloodType = patient.BloodType?.Value, Weight = patient.Weight?.Value, Status = Enum.GetName(patient.Status) });
        }
    }
}
