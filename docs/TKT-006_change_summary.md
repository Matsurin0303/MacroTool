# TKT-006 修正サマリ

## 対応内容
- `FindImage` の画像ソースを v7 に合わせて `CapturedBitmap` / `FilePath` の2種類に限定
- `Variable` / `Embedded` / その他の画像ソース種別を本版対象外として明記
- CSV / Macro / Domain の各仕様へ同じ制約を反映

## 修正対象
- `04_Domain/Domain_Model.md`
- `07_FileFormats/CSV_Schema_v1.0.md`
- `07_FileFormats/CSV_Import_Spec_v1.0.0.md`
- `07_FileFormats/CSV_Export_Spec_v1.0.0.md`
- `07_FileFormats/CSV_Column_To_DTO_Mapping.md`
- `07_FileFormats/Macro_FileFormat_Spec.md`

## 補足
- `BitmapKind=CapturedBitmap` の CSV 上の具体表現は、v7 だけでは確定できないため未確定事項として残しています。
- ただし、画像ソース種別そのものは `CapturedBitmap / FilePath` に固定しました。
