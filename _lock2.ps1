$p = 'D:\VicZero\Coding\Project-Ruin\Assets\_Game\Docs\Project-Ruin_CSV_작성_가이드.xlsx'
try {
  $s = [System.IO.File]::Open($p, 'Open', 'ReadWrite', 'None')
  $s.Close()
  'FILE_STATE: free'
} catch {
  'FILE_STATE: in-use (close it in Excel)'
}
