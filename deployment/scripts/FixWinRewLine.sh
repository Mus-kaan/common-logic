set -e

dos2unix(){
  tr -d '\r' < "$1" > t
  mv -f t "$1"
}

CurDir="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

for script in "$CurDir"/*.sh
do
  echo "dos2unix $script"
  dos2unix $script
  chmod +x $script
done