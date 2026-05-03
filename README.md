# ServerCore
Socket Async Event Args (SAEA) 방식의 네트워크 엔진 라이브러리

# 아키텍처 구조

## Buffers

### RecvBuffer.cs
<img width="1608" height="409" alt="Image" src="https://github.com/user-attachments/assets/a3a012b9-bc58-4729-87ce-bda051c42893" />

- 패킷 수신 버퍼
- ArraySegment<byte> ReadSegment
  - 수신된 데이터 중 처리되지 않은 데이터
- ArraySegment<byte> WriteSegment
  -  소켓을 통해 전송 받을 버퍼
- int DataSize
  - ReadSegment 사이즈
- int FreeSize
  - WriteSegment 사이즈
- bool OnRead(int readSize)
  - ReadSegment에서 처리한 바이트만큼 ReadPos 포인터 이동 (DataSize보다 크면 false)
- bool OnWrite(int writeSize)
  - WriteSegment에서 처리한 바이트만큼 WritePos 포인터 이동 (FreeSize보다 크면 false)
- void Clean()
  - 포인터가 어느 정도 차면 ReadSegment를 배열의 맨 앞으로 복사 후 포인터 조정
- void Reset()
  - 버퍼 리셋, 각 포인터 0으로 초기화
  
### SendBuffer.cs
<img width="1614" height="382" alt="Image" src="https://github.com/user-attachments/assets/141931f7-c442-45b9-a3e7-a2326c31edf3" />

- 패킷 전송 버퍼
- int FreeSize : 버퍼의 남은 사이즈
- ArraySegment<byte>? Open(int reserveSize) :
  - 전송할 패킷을 직렬하기 위해 버퍼에서 특정 크기만큼 예약
- ArraySegment<byte>? Close(int usedCount) :
  - 직렬화한 크기만큼 버퍼 슬라이싱 및 반환, UsedSize 포인터 이동

### SendBufferHandler.cs
<img width="1602" height="383" alt="Image" src="https://github.com/user-attachments/assets/660bd5ce-6cd3-4e2e-a29e-834826b3f3b3" />

- SendBuffer를 다루는 정적 클래스
- ThreadLocal<SendBuffer>
  - 세션에서 패킷을 전송할 때 랜덤한 쓰레드에서 전송하게 됨
  - 각 쓰레드별로 별도의 SendBuffer를 가지도록 하여 버퍼 안정화
- ArraySegment<byte> Open(int reserveSize)
  - SendBuffer의 Open 호출 및 반환
  - 현재 SendBuffer의 FreeSize보다 크면 SendBuffer 반납 및 새로운 SendBuffer 대여
  - SendBuffer의 버퍼사이즈보다 큰 크기로 예약시 예외 발생
- SendBufferWrapper Close(int usedSize)
  - SendBuffer의 Close 호출 -> SendBufferWrapper로 감싼 후 반환
  - 현재 SendBuffer의 FreeSize보다 크면 예외 발생(Open 후 Close 강제)

### SendBufferWrapper
- SendBuffer와 ArraySegment<byte>를 담고 있는 구조체
- Session에서 패킷을 전송할 때 패킷이 어떤 버퍼에서 작성되었는지 확인함으로써 SendBuffer 풀링 가능

### SendBufferPool
- SendBuffer 풀링용 정적 클래스

## Sessions

### Session.cs
- 추상 클래스
- abstract void OnConnect()
  - 세션 활성화시 호출되어 세션 초반 로직 구현
- abstract void OnDisconnect()
  - 세션 종료시 호출되어 마무리 및 세션 풀 반납 등 구현
- abstract int OnRecv(ArraySegment<byte> segment)
  - Recieve 이벤트 발생시 전송받은 패킷을 처리하고 처리한 사이즈 반환 (이후 RecvBuffer의 OnRead 호출)
- abstract int OnSend(int numOfBytes)
  - 전송한 바이트 카운트를 모니터링하는 메서드
- RegisterSend()
  - 소켓에서 데이터를 수신할 때 이벤트를 발생하도록 등록 
- 가독성과 유지보수를 위해 SendSession.cs, RecvSession.cs, SessionMain.cs로 나누어짐
- _sendingList와 _pendingList 두 개의 리스트 참조를 스왑하며 lock 시간 단축

### SessionList
- ArraySegment가 구조체임에도 참조 값을 가지고 있어 List<ArraySegment<byte>> 의 Clear를 수행할 때 O(N)의 시간이 걸리는 문제가 있음
- ArraySegment가 참조하는 배열은 풀링하여 관리하므로 리스트가 Clear될 때 참조를 해제할 필요가 없음
- Clear에서 포인터만 0으로 이동하도록 수정
- this[index], Clear, Count, Add 정도만 구현

### 세션 전송 플로우

- 수신 흐름
<img width="1674" height="942" alt="Image" src="https://github.com/user-attachments/assets/b69d16ad-f28d-4600-ab2a-fa48a12c621b" />
<br></br>

- 전송 흐름
<img width="1648" height="937" alt="Image" src="https://github.com/user-attachments/assets/1224dbeb-8071-4bd7-ab0d-adee059a0176" />

### SessionPool.cs
- 세션 풀링용 정적 클래스

## Listeners

### SocketListener 
- 추상 클래스
- abstract void OnStart()
  - Listener 활성화 시 호출, 로그 출력
- abstract void Quit(object log)
  - 서버를 종료할 때 호출할 클래스
- abstract void OnRegister()
  - Start가 끝날 때 호출, 로그 출력

- 흐름
<img width="1656" height="932" alt="Image" src="https://github.com/user-attachments/assets/1416f1eb-6972-44bf-8039-4dfb1567bcdf" />

